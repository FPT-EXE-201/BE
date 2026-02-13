using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phần B.IV — Khám bệnh. Ghi nhận mỗi lần khám.
/// Đây là phần CORE, bắt buộc mỗi buổi khám.
/// </summary>
public class ExaminationDto
{
    /// <summary>Sinh hiệu (Vital Signs)</summary>
    [JsonPropertyName("vitalSigns")]
    public VitalSignsDto? VitalSigns { get; set; }

    /// <summary>Khám tổng quát</summary>
    [JsonPropertyName("general")]
    public GeneralExaminationDto? General { get; set; }

    /// <summary>Khám sản khoa</summary>
    [JsonPropertyName("obstetric")]
    public ObstetricExaminationDto? Obstetric { get; set; }
}

/// <summary>Sinh hiệu — các chỉ số đo tại phòng khám</summary>
public class VitalSignsDto
{
    /// <summary>Mạch (lần/phút)</summary>
    [JsonPropertyName("pulseBpm")]
    public int? PulseBpm { get; set; }

    /// <summary>Nhiệt độ (°C)</summary>
    [JsonPropertyName("temperatureCelsius")]
    public decimal? TemperatureCelsius { get; set; }

    /// <summary>Huyết áp tâm thu (mmHg)</summary>
    [JsonPropertyName("bloodPressureSystolic")]
    public int? BloodPressureSystolic { get; set; }

    /// <summary>Huyết áp tâm trương (mmHg)</summary>
    [JsonPropertyName("bloodPressureDiastolic")]
    public int? BloodPressureDiastolic { get; set; }

    /// <summary>Nhịp thở (lần/phút)</summary>
    [JsonPropertyName("respiratoryRateBpm")]
    public int? RespiratoryRateBpm { get; set; }

    /// <summary>Cân nặng (kg)</summary>
    [JsonPropertyName("weightKg")]
    public decimal? WeightKg { get; set; }

    /// <summary>Chiều cao (cm)</summary>
    [JsonPropertyName("heightCm")]
    public decimal? HeightCm { get; set; }
}

/// <summary>Khám tổng quát — tinh thần, phù, protein niệu</summary>
public class GeneralExaminationDto
{
    /// <summary>Tinh thần: "alert" | "coma" | "other"</summary>
    [JsonPropertyName("mentalStatus")]
    public string? MentalStatus { get; set; }

    /// <summary>Ghi rõ nếu tinh thần "other"</summary>
    [JsonPropertyName("mentalStatusNote")]
    public string? MentalStatusNote { get; set; }

    /// <summary>Phù</summary>
    [JsonPropertyName("edema")]
    public bool? Edema { get; set; }

    /// <summary>Protein niệu</summary>
    [JsonPropertyName("urineProtein")]
    public bool? UrineProtein { get; set; }

    /// <summary>Giá trị protein niệu (g/l) nếu có</summary>
    [JsonPropertyName("urineProteinValue")]
    public decimal? UrineProteinValue { get; set; }
}

/// <summary>Khám sản khoa — tử cung, ngôi thai, tim thai, cổ tử cung, ối</summary>
public class ObstetricExaminationDto
{
    /// <summary>Sẹo mổ cũ</summary>
    [JsonPropertyName("oldScar")]
    public bool? OldScar { get; set; }

    /// <summary>Đau vết mổ</summary>
    [JsonPropertyName("scarPainful")]
    public bool? ScarPainful { get; set; }

    /// <summary>Khung chậu: "normal" | "abnormal"</summary>
    [JsonPropertyName("pelvis")]
    public string? Pelvis { get; set; }

    /// <summary>Chiều cao tử cung (cm)</summary>
    [JsonPropertyName("fundusHeightCm")]
    public decimal? FundusHeightCm { get; set; }

    /// <summary>Vòng bụng (cm)</summary>
    [JsonPropertyName("abdominalCircumferenceCm")]
    public decimal? AbdominalCircumferenceCm { get; set; }

    /// <summary>Ngôi thai: "normal" | "abnormal"</summary>
    [JsonPropertyName("fetalPresentation")]
    public string? FetalPresentation { get; set; }

    /// <summary>Ghi rõ nếu ngôi thai bất thường. VD: "Ngôi mông"</summary>
    [JsonPropertyName("fetalPresentationNote")]
    public string? FetalPresentationNote { get; set; }

    /// <summary>Cơn co tử cung</summary>
    [JsonPropertyName("uterineContraction")]
    public bool? UterineContraction { get; set; }

    /// <summary>Tần số cơn co (cơn/10 phút)</summary>
    [JsonPropertyName("uterineContractionFrequency")]
    public int? UterineContractionFrequency { get; set; }

    /// <summary>Tim thai nghe được</summary>
    [JsonPropertyName("fetalHeartbeat")]
    public bool? FetalHeartbeat { get; set; }

    /// <summary>Nhịp tim thai (lần/phút)</summary>
    [JsonPropertyName("fetalHeartRateBpm")]
    public int? FetalHeartRateBpm { get; set; }

    // ── Cổ tử cung ──

    /// <summary>Cổ tử cung: "closed" | "effaced" | "dilated"</summary>
    [JsonPropertyName("cervix")]
    public string? Cervix { get; set; }

    /// <summary>Cổ tử cung mở (cm) — khi cervix = "dilated"</summary>
    [JsonPropertyName("cervixDilationCm")]
    public decimal? CervixDilationCm { get; set; }

    // ── Đầu ối ──

    /// <summary>Đầu ối: "bulging" (phồng) | "flat" (dẹt) | "pear" (quả lê)</summary>
    [JsonPropertyName("amnioticSac")]
    public string? AmnioticSac { get; set; }

    /// <summary>Tình trạng màng ối: "intact" | "leaking" (rỉ) | "ruptured" (vỡ)</summary>
    [JsonPropertyName("membraneStatus")]
    public string? MembraneStatus { get; set; }

    /// <summary>Thời gian vỡ/rỉ ối (HH:mm). VD: "14:30"</summary>
    [JsonPropertyName("membraneRuptureTime")]
    public string? MembraneRuptureTime { get; set; }

    // ── Nước ối ──

    /// <summary>Nước ối: "clear" (trong) | "green" (xanh bản) | "bloody" (lẫn máu)</summary>
    [JsonPropertyName("amnioticFluid")]
    public string? AmnioticFluid { get; set; }
}
