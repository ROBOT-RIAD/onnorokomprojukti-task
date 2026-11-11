using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public HoldsController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpGet("{holdId}")]
    public async Task<IActionResult> GetHoldStatus([FromRoute] string holdId)
    {
        var hold = await _admissionService.GetHoldStatusAsync(holdId);
        if (hold == null)
        {
            return NotFound(new { error = $"Hold with id '{holdId}' was not found." });
        }
        return Ok(hold);
    }

    [HttpPost("{holdId}/confirm")]
    public async Task<IActionResult> ConfirmHold(
        [FromRoute] string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return BadRequest(new { error = "X-Student-Id header is required." });
        }

        var result = await _admissionService.ConfirmHoldAsync(holdId, studentId);
        return Ok(result);
    }

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> CancelHold(
        [FromRoute] string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return BadRequest(new { error = "X-Student-Id header is required." });
        }

        await _admissionService.CancelHoldAsync(holdId, studentId);
        return NoContent();
    }
}
