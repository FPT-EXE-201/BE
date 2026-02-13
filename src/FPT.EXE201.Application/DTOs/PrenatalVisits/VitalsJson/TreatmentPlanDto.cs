using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phần B.VII — Kế hoạch điều trị (thuốc, chăm sóc).
/// </summary>
public class TreatmentPlanDto
{
    /// <summary>Thuốc, chăm sóc. VD: "Methyldopa 250mg x 3 lần/ngày"</summary>
    [JsonPropertyName("medication")]
    public string? Medication { get; set; }

    /// <summary>Hướng điều trị tiếp theo. VD: "Theo dõi HA mỗi ngày, tái khám 1 tuần"</summary>
    [JsonPropertyName("nextSteps")]
    public string? NextSteps { get; set; }

    /// <summary>Tư vấn GDSK cho người bệnh và thân nhân</summary>
    [JsonPropertyName("healthEducation")]
    public bool? HealthEducation { get; set; }

    /// <summary>Nội dung tư vấn. VD: "Hướng dẫn đo HA tại nhà"</summary>
    [JsonPropertyName("healthEducationNote")]
    public string? HealthEducationNote { get; set; }
}
