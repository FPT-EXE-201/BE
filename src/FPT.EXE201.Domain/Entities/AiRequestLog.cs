using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Logs every AI API call. From Week 5 schema (ai_request_logs table).
/// Entity created in Week 7 for meal plan generation tracking + rate limiting.
/// 1 record per week-chunk per generation.
/// </summary>
public class AiRequestLog : BaseEntity
{
    public AiFeature Feature { get; set; }
    public Guid? PregnancyId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TemplateId { get; set; }
    public AiRequestStatus Status { get; set; } = AiRequestStatus.Pending;
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public int? TokensInput { get; set; }
    public int? TokensOutput { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public Pregnancy? Pregnancy { get; set; }
    public User? User { get; set; }
    public AiPromptTemplate? Template { get; set; }
}
