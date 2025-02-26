// XYZUniversityAPI/Models/Student.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XYZUniversityAPI.Models
{
    public class Student
    {
        [Key]
        [Required]
        [MaxLength(50)]
        public string StudentId { get; set; } = string.Empty; // Unique student identifier, e.g., "2020-TWC-1223"

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public bool IsEnrolled { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FeeBalance { get; set; }
    }
}