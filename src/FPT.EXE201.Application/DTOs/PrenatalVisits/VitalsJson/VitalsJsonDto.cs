using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phiếu Khám Thai (MS: 51/BV2) — Bộ Y tế Việt Nam.
/// Strongly-typed schema cho VitalsJson field.
/// Dùng cho: FE form render, AI/OCR extraction output, DB storage.
/// </summary>
public class VitalsJsonDto
{
    // ═══ A. THÔNG TIN CHUNG ═══
    [JsonPropertyName("generalInfo")]
    public GeneralInfoDto? GeneralInfo { get; set; }

    // ═══ B.I. THÔNG TIN LẦN KHÁM TRƯỚC ═══
    [JsonPropertyName("previousVisit")]
    public PreviousVisitInfoDto? PreviousVisit { get; set; }

    // ═══ B.II. HỎI BỆNH ═══
    [JsonPropertyName("interview")]
    public InterviewDto? Interview { get; set; }

    // ═══ B.III. TIỀN SỬ BỆNH ═══
    [JsonPropertyName("medicalHistory")]
    public MedicalHistoryDto? MedicalHistory { get; set; }

    // ═══ B.IV. KHÁM BỆNH ═══
    [JsonPropertyName("examination")]
    public ExaminationDto? Examination { get; set; }

    // ═══ B.VI. CHẨN ĐOÁN ═══
    [JsonPropertyName("diagnosis")]
    public DiagnosisDto? Diagnosis { get; set; }

    // ═══ B.VII. KẾ HOẠCH ĐIỀU TRỊ ═══
    [JsonPropertyName("treatmentPlan")]
    public TreatmentPlanDto? TreatmentPlan { get; set; }

    // ═══ B.VIII. TIÊN LƯỢNG ═══
    /// <summary>"normal" | "risky" | "cesarean_indicated"</summary>
    [JsonPropertyName("prognosis")]
    public string? Prognosis { get; set; }

    // ═══ B.IX. LẦN KHÁM KẾ TIẾP ═══
    [JsonPropertyName("nextAppointment")]
    public NextAppointmentDto? NextAppointment { get; set; }
}
