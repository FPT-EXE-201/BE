namespace FPT.EXE201.Application.AI.ExtractionModels;

/// <summary>
/// RAG context retrieved from database.
/// Injected vào AI prompt để cung cấp context cho extraction chính xác hơn.
/// ⚡ Reusable cho Nutrition Planning AI (Week sau).
/// </summary>
public class PregnancyContext
{
    public Guid PregnancyId { get; set; }
    public int? CurrentGestationalWeek { get; set; }
    public string? PregnancyStatus { get; set; }

    /// <summary>Danh sách bệnh lý đã biết (từ PregnancyConditions).</summary>
    public List<string> KnownConditions { get; set; } = new();

    /// <summary>Tóm tắt medical record gần nhất (cho consistency check).</summary>
    public string? PreviousRecordSummary { get; set; }
}
