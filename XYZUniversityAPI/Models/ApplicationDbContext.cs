using Microsoft.EntityFrameworkCore;
using XYZUniversityAPI.Models;

namespace XYZUniversityAPI.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>()
                .HasKey(s => s.StudentId);

            modelBuilder.Entity<Payment>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TransactionId)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .Property(p => p.StudentId)
                .HasMaxLength(50);

            modelBuilder.Entity<Payment>()
                .HasOne<Student>()
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);
        }
    }
}