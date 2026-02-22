using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api/motivational")]
public class MotivationalController : BaseApiController
{
    private readonly IMotivationalService _motivationalService;

    public MotivationalController(IMotivationalService motivationalService)
    {
        _motivationalService = motivationalService;
    }

    /// <summary>
    /// Lấy thông điệp động viên theo tuần thai.  (Public – không cần đăng nhập)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int week,
        [FromQuery] string? category = null,
        [FromQuery] string lang = "vi",
        CancellationToken ct = default)
    {
        var result = await _motivationalService.GetByWeekAsync(week, category, lang, ct);
        return Success(result);
    }
}
