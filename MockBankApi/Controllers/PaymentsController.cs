using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MockBankAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MockBankAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ILogger<PaymentsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MockBankDbContext _context;

        public PaymentsController(ILogger<PaymentsController> logger, IHttpClientFactory httpClientFactory, MockBankDbContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        [HttpPost("validate-student/{universityCode}")]
        public async Task<IActionResult> ValidateStudent(string universityCode, [FromBody] StudentValidationRequest request)
        {
            var config = await GetUniversityConfig(universityCode);
            if (config == null)
            {
                _logger.LogWarning("University not found: {UniversityCode}", universityCode);
                await LogAudit("ValidateStudent", request, 400, "University not found");
                return BadRequest(new { error = "University not found" });
            }

            var client = CreateClient(config);
            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

            try
            {
                _logger.LogInformation("Sending validation to {University}: {Request}", universityCode, JsonSerializer.Serialize(request));
                var response = await client.PostAsync("/api/students/validate", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Validation response from {University}: {Response}", universityCode, responseContent);
                await LogAudit("ValidateStudent", request, 200);
                return Ok(JsonSerializer.Deserialize<object>(responseContent));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to validate student for {University}.", universityCode);
                await LogAudit("ValidateStudent", request, 500, ex.Message);
                return StatusCode(500, new { error = "Failed to validate student", details = ex.Message });
            }
        }

        [HttpPost("send-notification/{universityCode}")]
        public async Task<IActionResult> SendNotification(string universityCode, [FromBody] List<PaymentRequest> requests)
        {
            var config = await GetUniversityConfig(universityCode);
            if (config == null)
            {
                _logger.LogWarning("University not found: {UniversityCode}", universityCode);
                await LogAudit("SendNotification", requests, 400, "University not found");
                return BadRequest(new { error = "University not found" });
            }

            var client = CreateClient(config);
            var content = new StringContent(JsonSerializer.Serialize(requests), System.Text.Encoding.UTF8, "application/json");

            foreach (var request in requests)
            {
                var payment = new Payment
                {
                    TransactionId = request.TransactionId,
                    StudentId = request.StudentId,
                    Amount = request.Amount,
                    PaymentDate = request.PaymentDate,
                    Status = "Sent",
                    ResponseDetails = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Payments.Add(payment);
            }

            try
            {
                _logger.LogInformation("Sending payment notification to {University}: {Request}", universityCode, JsonSerializer.Serialize(requests));
                var response = await client.PostAsync("/api/payments/notification", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Payment notification sent to {University}: {Response}", universityCode, responseContent);
                await _context.SaveChangesAsync();
                await LogAudit("SendNotification", requests, 200);
                return Ok(JsonSerializer.Deserialize<object>(responseContent));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to send payment notification to {University}.", universityCode);
                await LogAudit("SendNotification", requests, 500, ex.Message);
                return StatusCode(500, new { error = "Failed to process payment notification", details = ex.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] List<PaymentResponse> responses)
        {
            if (responses == null || responses.Count == 0)
            {
                _logger.LogWarning("Received invalid webhook payload.");
                await LogAudit("ReceiveWebhook", null, 400, "Webhook payload cannot be empty");
                return BadRequest(new { error = "Webhook payload cannot be empty" });
            }

            foreach (var response in responses)
            {
                _logger.LogInformation("Received webhook: TransactionId={TransactionId}, Status={Status}", response.TransactionId, response.Status);
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == response.TransactionId);
                if (payment != null)
                {
                    payment.Status = response.Status;
                    payment.ResponseDetails = JsonSerializer.Serialize(response);
                    payment.UpdatedAt = DateTime.UtcNow;
                    _context.Payments.Update(payment);
                }
                else
                {
                    _logger.LogWarning("Payment not found for webhook: TransactionId={TransactionId}", response.TransactionId);
                }
            }
            await _context.SaveChangesAsync();
            await LogAudit("ReceiveWebhook", responses, 200);

            return Ok(new { message = "Webhook received successfully" });
        }

        private async Task<UniversityConfig?> GetUniversityConfig(string universityCode)
        {
            return await _context.UniversityConfigs
                .FirstOrDefaultAsync(u => u.UniversityCode == universityCode);
        }

        private HttpClient CreateClient(UniversityConfig config)
        {
            var client = _httpClientFactory.CreateClient();
            if (string.IsNullOrEmpty(config.BaseUrl))
            {
                throw new ArgumentException("BaseUrl cannot be null or empty.", nameof(config));
            }
            client.BaseAddress = new Uri(config.BaseUrl);
            client.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
            return client;
        }

        private async Task LogAudit(string endpoint, object? requestPayload, int responseStatus, string? errorMessage = null)
        {
            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                User = "MockBank",
                Endpoint = endpoint,
                RequestPayload = requestPayload != null ? JsonSerializer.Serialize(requestPayload) : null,
                ResponseStatus = responseStatus,
                ErrorMessage = errorMessage
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}