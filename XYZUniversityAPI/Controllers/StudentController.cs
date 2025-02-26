using Microsoft.AspNetCore.Mvc;
using XYZUniversityAPI.Repositories;
using XYZUniversityAPI.Models;

namespace XYZUniversityAPI.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentRepository _studentRepository;

        public StudentController(IStudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateStudent([FromBody] Student request)
        {
            if (string.IsNullOrEmpty(request.StudentId))
                return BadRequest(new { error = "Student ID is required." });

            var student = await _studentRepository.GetStudentByIdAsync(request.StudentId);
            if (student == null || !student.IsEnrolled)
                return NotFound(new { error = "Student not enrolled." });

            return Ok(new { isValid = true, enrollmentStatus = "Active" });
        }
    }
}
