using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XYZUniversityAPI.Models
{
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Ensures EF handles ID generation
        public int Id { get; set; } // Auto-generated primary key

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(100)]
        public string User { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Endpoint { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? RequestPayload { get; set; }

        [Required]
        public int ResponseStatus { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }
    }
}