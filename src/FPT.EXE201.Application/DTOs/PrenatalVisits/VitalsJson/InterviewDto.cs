using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phần B.II — Hỏi bệnh. Ghi nhận mỗi lần khám.
/// </summary>
public class InterviewDto
{
    /// <summary>Lý do vào viện</summary>
    [JsonPropertyName("reasonForVisit")]
    public string? ReasonForVisit { get; set; }

    /// <summary>Lần có thai thứ</summary>
    [JsonPropertyName("pregnancyNumber")]
    public int? PregnancyNumber { get; set; }

    /// <summary>Số lần khám thai (bao gồm cả lần này)</summary>
    [JsonPropertyName("totalVisitCount")]
    public int? TotalVisitCount { get; set; }

    /// <summary>Ngày đầu kỳ kinh cuối (yyyy-MM-dd)</summary>
    [JsonPropertyName("lastMenstrualPeriodDate")]
    public string? LastMenstrualPeriodDate { get; set; }

    /// <summary>Tuổi thai (tuần)</summary>
    [JsonPropertyName("gestationalWeek")]
    public int? GestationalWeek { get; set; }

    /// <summary>Ngày dự kiến sinh (yyyy-MM-dd)</summary>
    [JsonPropertyName("expectedDeliveryDate")]
    public string? ExpectedDeliveryDate { get; set; }

    /// <summary>Diễn biến lâm sàng</summary>
    [JsonPropertyName("clinicalProgress")]
    public string? ClinicalProgress { get; set; }

    /// <summary>Toàn thân: "normal" | "abnormal"</summary>
    [JsonPropertyName("generalCondition")]
    public string? GeneralCondition { get; set; }

    /// <summary>Ghi rõ nếu bất thường</summary>
    [JsonPropertyName("generalConditionNote")]
    public string? GeneralConditionNote { get; set; }

    /// <summary>Số mũi tiêm phòng uốn ván đã tiêm (bao gồm cả các lần mang thai trước nếu có)</summary>
    [JsonPropertyName("tetanusVaccineHistory")]
    public int? TetanusVaccineHistory { get; set; }
}
