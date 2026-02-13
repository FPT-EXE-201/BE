using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phần B.VI — Chẩn đoán (Tên bệnh kèm theo mã ICD).
/// </summary>
public class DiagnosisDto
{
    /// <summary>Tên bệnh / chẩn đoán. VD: "Thai 28 tuần, tiền sản giật nhẹ"</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Mã ICD. VD: "O14.0"</summary>
    [JsonPropertyName("icdCode")]
    public string? IcdCode { get; set; }
}
