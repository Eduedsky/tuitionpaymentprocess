using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XYZUniversityAPI.Models
{
    public class PaymentRequest
    {
        public string? TransactionId { get; set; }
        public string? StudentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class PaymentResponse
    {
        public string? TransactionId { get; set; }
        public string? Status { get; set; }
    }
}
