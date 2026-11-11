using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Exceptions;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public ClassesController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return BadRequest(new { error = errors });
        }

        var created = await _admissionService.CreateClassAsync(request);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet]
    public async Task<IActionResult> GetClasses()
    {
        var classes = await _admissionService.GetClassesAsync();
        return Ok(classes);
    }

    [HttpGet("{classId}")]
    public async Task<IActionResult> GetClassById(string classId)
    {
        var classSummary = await _admissionService.GetClassByIdAsync(classId);
        if (classSummary == null)
        {
            return NotFound(new { error = $"Class with id '{classId}' not found." });
        }
        return Ok(classSummary);
    }

    [HttpPost("{classId}/holds")]
    public async Task<IActionResult> CreateHold(
        [FromRoute] string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return BadRequest(new { error = "X-Student-Id header is required." });
        }

        var hold = await _admissionService.CreateHoldAsync(classId, studentId);
        return StatusCode(StatusCodes.Status201Created, hold);
    }
}
