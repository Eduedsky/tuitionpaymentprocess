using XYZUniversityAPI.Models;

namespace XYZUniversityAPI.Repositories
{
    public interface IStudentRepository
    {
        Task<Student?> GetStudentByIdAsync(string studentId);
        Task AddAsync(Student student);
        Task UpdateAsync(Student student);
    }
}
