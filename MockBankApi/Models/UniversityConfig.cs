using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MockBankAPI.Models;

namespace MockBankAPI.Models
{
    public class UniversityConfig
    {
        public int Id { get; set; }
        public string? UniversityCode { get; set; }
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
    }
}