using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockBankAPI.Models
{
    public class StudentValidationRequest
    {
        public string? StudentId { get; set; }
    }
}