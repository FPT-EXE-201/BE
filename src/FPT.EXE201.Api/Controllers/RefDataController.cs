using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Api.Controllers;

/// <summary>
/// Public endpoints cho reference/lookup data.
/// Không cần authentication — user cần xem danh mục trước khi đăng ký.
/// </summary>
[Route("api/ref")]
public class RefDataController : BaseApiController
{
    private readonly IRefDataService _refDataService;

    public RefDataController(IRefDataService refDataService)
    {
        _refDataService = refDataService;
    }

    /// <summary>
    /// Lấy danh mục bệnh lý thai kỳ.
    /// Frontend gọi khi hiển thị dropdown "Chọn bệnh lý".
    /// </summary>
    [HttpGet("pregnancy-conditions")]
    public async Task<IActionResult> GetConditions([FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _refDataService.GetActiveConditionsAsync(lang, ct);
        return Success(result);
    }

    /// <summary>
    /// Lấy danh mục loại xét nghiệm.
    /// Optional filter by category: LAB, IMAGING, OTHER.
    /// </summary>
    [HttpGet("test-types")]
    public async Task<IActionResult> GetTestTypes([FromQuery] string lang = "vi", [FromQuery] string? category = null, CancellationToken ct = default)
    {
        var result = await _refDataService.GetActiveTestTypesAsync(lang, category, ct);
        return Success(result);
    }

    /// <summary>
    /// Trả về tất cả enum values để FE biết các giá trị hợp lệ.
    /// Mỗi enum trả về danh sách { value (int), name (string) }.
    /// </summary>
    [HttpGet("enums")]
    public IActionResult GetEnums()
    {
        var result = new Dictionary<string, object>
        {
            ["babyGender"] = ToEnumList<BabyGender>(),
            ["pregnancyStatus"] = ToEnumList<PregnancyStatus>(),
            ["pregnancyType"] = ToEnumList<PregnancyType>(),
            ["dueDateSource"] = ToEnumList<DueDateSource>(),
            ["deliveryMethod"] = ToEnumList<DeliveryMethod>(),
            ["conditionSeverity"] = ToEnumList<ConditionSeverity>(),
            ["visitType"] = ToEnumList<VisitType>(),
        };
        return Success(result);
    }

    /// <summary>
    /// Trả về enum values cho 1 enum cụ thể theo tên.
    /// </summary>
    [HttpGet("enums/{enumName}")]
    public IActionResult GetEnumByName(string enumName)
    {
        var enums = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["babyGender"] = ToEnumList<BabyGender>(),
            ["pregnancyStatus"] = ToEnumList<PregnancyStatus>(),
            ["pregnancyType"] = ToEnumList<PregnancyType>(),
            ["dueDateSource"] = ToEnumList<DueDateSource>(),
            ["deliveryMethod"] = ToEnumList<DeliveryMethod>(),
            ["conditionSeverity"] = ToEnumList<ConditionSeverity>(),
            ["visitType"] = ToEnumList<VisitType>(),
        };

        if (!enums.TryGetValue(enumName, out var values))
            return NotFound();

        return Success(values);
    }

    private static List<object> ToEnumList<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>()
            .Select(e => (object)new { value = Convert.ToInt32(e), name = e.ToString() })
            .ToList();
    }
}
