using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MockBankAPI.Models;

namespace MockBankAPI.Models
{
    public class MockBankDbContext : DbContext
    {
        public MockBankDbContext(DbContextOptions<MockBankDbContext> options) : base(options) { }
        public DbSet<UniversityConfig> UniversityConfigs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Payment> Payments { get; set; }
    }
}