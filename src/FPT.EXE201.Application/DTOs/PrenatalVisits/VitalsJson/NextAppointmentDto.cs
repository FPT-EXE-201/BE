using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phần B.IX — Lần khám kế tiếp.
/// FE có thể tạo reminder từ thông tin này.
/// </summary>
public class NextAppointmentDto
{
    /// <summary>Hẹn tái khám (yyyy-MM-dd)</summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>Lưu ý. VD: "Mang theo kết quả XN máu"</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>Người khám: "obstetrician" | "midwife" | "pediatric_nurse" | "other"</summary>
    [JsonPropertyName("examinerType")]
    public string? ExaminerType { get; set; }
}
