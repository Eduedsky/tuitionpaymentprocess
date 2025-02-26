using Microsoft.AspNetCore.Mvc;
using XYZUniversityAPI.Models;
using XYZUniversityAPI.Services;
using System.Text.Json;

namespace XYZUniversityAPI.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentController : ControllerBase
    {
        private readonly StudentService _studentService;
        private readonly ApplicationDbContext _context;

        public StudentController(StudentService studentService, ApplicationDbContext context)
        {
            _studentService = studentService;
            _context = context;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateStudent([FromBody] ValidateStudentRequest request)
        {
            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                User = "MockBank",
                Endpoint = "/api/students/validate",
                RequestPayload = JsonSerializer.Serialize(request),
                ResponseStatus = 200
            };

            try
            {
                if (string.IsNullOrEmpty(request.StudentId))
                {
                    auditLog.ResponseStatus = 400;
                    auditLog.ErrorMessage = "Student ID is required.";
                    _context.AuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();
                    return BadRequest(new { error = auditLog.ErrorMessage });
                }

                var student = await _studentService.ValidateStudentAsync(request.StudentId, Request.Headers["X-API-Key"]);
                var response = new
                {
                    isValid = true,
                    enrollmentStatus = "Active",
                    studentId = student.StudentId,
                    studentName = student.StudentName,
                    feeBalance = student.FeeBalance
                };
                auditLog.ResponseStatus = 200;
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                auditLog.ResponseStatus = 404;
                auditLog.ErrorMessage = ex.Message;
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                auditLog.ResponseStatus = 401;
                auditLog.ErrorMessage = ex.Message;
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                auditLog.ResponseStatus = 400;
                auditLog.ErrorMessage = ex.Message;
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                auditLog.ResponseStatus = 500;
                auditLog.ErrorMessage = "Internal server error: " + ex.Message;
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                return StatusCode(500, new { error = auditLog.ErrorMessage });
            }
        }
    }

    public class ValidateStudentRequest
    {
        public string? StudentId { get; set; }
    }
}