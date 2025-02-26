using XYZUniversityAPI.Models;
using XYZUniversityAPI.Repositories;
using Microsoft.Extensions.Logging;

namespace XYZUniversityAPI.Services
{
    public class PaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IPaymentRepository paymentRepository, IStudentRepository studentRepository, ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<List<PaymentResponse>> ProcessPaymentsAsync(List<PaymentRequest> requests)
        {
            var results = new List<PaymentResponse>();
            foreach (var request in requests.Take(100))
            {
                try
                {
                    var existing = await _paymentRepository.GetByTransactionIdAsync(request.TransactionId ?? string.Empty);
                    if (existing != null)
                    {
                        results.Add(new PaymentResponse { TransactionId = request.TransactionId, Status = "success" });
                        _logger.LogInformation("Payment {TransactionId} already processed", request.TransactionId);
                        continue;
                    }

                    var student = await _studentRepository.GetStudentByIdAsync(request.StudentId ?? string.Empty);
                    if (student == null || !student.IsEnrolled)
                    {
                        results.Add(new PaymentResponse { TransactionId = request.TransactionId, Status = "failed" });
                        _logger.LogWarning("Invalid student {StudentId} for payment {TransactionId}", request.StudentId, request.TransactionId);
                        continue;
                    }

                    var payment = new Payment
                    {
                        TransactionId = request.TransactionId!,
                        StudentId = request.StudentId!,
                        Amount = request.Amount,
                        PaymentDate = request.PaymentDate,
                        Status = "success",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _paymentRepository.AddAsync(payment);
                    results.Add(new PaymentResponse { TransactionId = request.TransactionId, Status = "success" });
                    _logger.LogInformation("Payment {TransactionId} processed", request.TransactionId);
                }
                catch (Exception ex)
                {
                    results.Add(new PaymentResponse { TransactionId = request.TransactionId, Status = "failed" });
                    _logger.LogError(ex, "Error processing payment {TransactionId}", request.TransactionId);
                }
            }
            return results;
        }
    }
}