using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phần B.I — Thông tin lần khám trước.
/// FE có thể auto-fill từ PrenatalVisit trước đó.
/// </summary>
public class PreviousVisitInfoDto
{
    /// <summary>Ngày khám trước (yyyy-MM-dd)</summary>
    [JsonPropertyName("visitDate")]
    public string? VisitDate { get; set; }

    /// <summary>Chẩn đoán lần trước</summary>
    [JsonPropertyName("diagnosis")]
    public string? Diagnosis { get; set; }

    /// <summary>Xử trí lần trước</summary>
    [JsonPropertyName("treatment")]
    public string? Treatment { get; set; }
}
