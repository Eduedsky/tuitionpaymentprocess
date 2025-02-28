using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MockBankAPI.Models;

namespace MockBankAPI.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? User { get; set; }
        public string? Endpoint { get; set; }
        public string? RequestPayload { get; set; }
        public int ResponseStatus { get; set; }
        public string? ErrorMessage { get; set; }
    }
}