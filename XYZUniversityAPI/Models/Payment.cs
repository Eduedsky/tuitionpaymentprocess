// XYZUniversityAPI/Models/Payment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XYZUniversityAPI.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; } // Auto-incrementing integer primary key

        [Required]
        [MaxLength(50)]
        public string TransactionId { get; set; } = string.Empty;// Unique transaction identifier

        [Required]
        [MaxLength(50)]
        public string StudentId { get; set; } // Foreign key to Student.Id

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // e.g., "Completed", "Failed"

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }
    }
}