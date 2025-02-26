using XYZUniversityAPI.Models;
using XYZUniversityAPI.Repositories;
using Microsoft.Extensions.Logging;

namespace XYZUniversityAPI.Services

{
    public class StudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<StudentService> _logger;

        public StudentService(IStudentRepository studentRepository, ILogger<StudentService> logger)
        {
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<Student> ValidateStudentAsync(string studentId, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new UnauthorizedAccessException("API key is required.");

            if (string.IsNullOrEmpty(studentId))
                throw new ArgumentNullException(nameof(studentId), "Student ID is required.");

            _logger.LogInformation("Validating student with ID: {StudentId}", studentId);

            var student = await _studentRepository.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                _logger.LogWarning("Student with ID {StudentId} not found in the database", studentId);
                throw new KeyNotFoundException($"Student with ID {studentId} not found in the school database.");
            }

            if (!student.IsEnrolled)
            {
                _logger.LogWarning("Student with ID {StudentId} is not enrolled", studentId);
                throw new InvalidOperationException($"Student with ID {studentId} is not currently enrolled.");
            }

            _logger.LogInformation("Student with ID {StudentId} validated successfully", studentId);
            return student;
        }
    }
}