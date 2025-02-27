using Microsoft.AspNetCore.Mvc;
using XYZUniversityAPI.Services;
using XYZUniversityAPI.Models;
using System.Text.Json;
using System.Net.Http;

namespace XYZUniversityAPI.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentService _paymentService;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PaymentsController(PaymentService paymentService, ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("notification")]
        public async Task<IActionResult> ProcessPayment([FromBody] List<PaymentRequest> requests)
        {
            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                User = "MockBank",
                Endpoint = "/api/payments/notification",
                RequestPayload = JsonSerializer.Serialize(requests),
                ResponseStatus = 200,
                ErrorMessage = null
            };

            try
            {
                if (requests == null || !requests.Any())
                {
                    auditLog.ResponseStatus = 400;
                    auditLog.ErrorMessage = "Invalid input data";
                    _context.AuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();
                    return BadRequest(new { error = auditLog.ErrorMessage });
                }

                var results = await _paymentService.ProcessPaymentsAsync(requests);
                auditLog.ResponseStatus = 200;
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Webhook notification
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-API-Key", _configuration["WebhookSettings:ApiKey"]);
                var webhookUrl = _configuration["WebhookSettings:MockBankUrl"];
                await client.PostAsJsonAsync(webhookUrl, results);

                return Ok(results);
            }
            catch (Exception ex)
            {
                auditLog.ResponseStatus = 500;
                auditLog.ErrorMessage = "Internal server error: " + ex.Message;
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                return StatusCode(500, new { error = auditLog.ErrorMessage });
            }
        }
    }

}