// XYZUniversityAPI/Repositories/IPaymentRepository.cs
using XYZUniversityAPI.Models;

namespace XYZUniversityAPI.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByTransactionIdAsync(string transactionId);
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
    }
}