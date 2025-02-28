using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockBankAPI.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string? TransactionId { get; set; }
        public string? StudentId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Status { get; set; } // e.g., "Sent", "Failed", "Successful"
        public string? ResponseDetails { get; set; } // Full response from XYZUniversityAPI
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } // Last status update time
    }
}