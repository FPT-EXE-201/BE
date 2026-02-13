using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phần B.III — Tiền sử bệnh.
/// Chủ yếu khai thác ở lần khám thai đầu tiên.
/// FE có thể ẩn ở các lần khám tiếp theo.
/// </summary>
public class MedicalHistoryDto
{
    /// <summary>3.1. Tiền sử bản thân</summary>
    [JsonPropertyName("personal")]
    public PersonalHistoryDto? Personal { get; set; }

    /// <summary>Tiền sử sản khoa</summary>
    [JsonPropertyName("obstetric")]
    public ObstetricHistoryDto? Obstetric { get; set; }

    /// <summary>Phụ khoa</summary>
    [JsonPropertyName("gynecology")]
    public GynecologyHistoryDto? Gynecology { get; set; }

    /// <summary>Sa tạng chậu</summary>
    [JsonPropertyName("pelvicOrganProlapse")]
    public bool? PelvicOrganProlapse { get; set; }

    /// <summary>Bệnh phụ khoa đã mắc và điều trị</summary>
    [JsonPropertyName("gynecologicalDiseaseNote")]
    public string? GynecologicalDiseaseNote { get; set; }

    /// <summary>3.2. Tiền sử gia đình</summary>
    [JsonPropertyName("family")]
    public FamilyHistoryDto? Family { get; set; }
}

/// <summary>Tiền sử bản thân — bệnh lý, dị ứng, phẫu thuật</summary>
public class PersonalHistoryDto
{
    /// <summary>Dị ứng</summary>
    [JsonPropertyName("allergy")]
    public bool? Allergy { get; set; }

    /// <summary>Biểu hiện dị ứng</summary>
    [JsonPropertyName("allergyNote")]
    public string? AllergyNote { get; set; }

    /// <summary>Tiền sử bệnh</summary>
    [JsonPropertyName("medicalHistory")]
    public bool? MedicalHistory { get; set; }

    /// <summary>Ghi đầy đủ nội dung tiền sử bệnh</summary>
    [JsonPropertyName("medicalHistoryNote")]
    public string? MedicalHistoryNote { get; set; }

    /// <summary>Bệnh huyết áp</summary>
    [JsonPropertyName("hypertension")]
    public bool? Hypertension { get; set; }

    /// <summary>Bệnh tim</summary>
    [JsonPropertyName("heartDisease")]
    public bool? HeartDisease { get; set; }

    /// <summary>Bệnh hô hấp</summary>
    [JsonPropertyName("respiratoryDisease")]
    public bool? RespiratoryDisease { get; set; }

    /// <summary>Bệnh tuyến giáp</summary>
    [JsonPropertyName("thyroidDisease")]
    public bool? ThyroidDisease { get; set; }

    /// <summary>Bệnh thận</summary>
    [JsonPropertyName("kidneyDisease")]
    public bool? KidneyDisease { get; set; }

    /// <summary>Đái tháo đường</summary>
    [JsonPropertyName("diabetes")]
    public bool? Diabetes { get; set; }

    /// <summary>Bệnh khác (ghi rõ)</summary>
    [JsonPropertyName("otherDiseases")]
    public string? OtherDiseases { get; set; }

    /// <summary>Thuốc đang dùng</summary>
    [JsonPropertyName("currentMedications")]
    public bool? CurrentMedications { get; set; }

    /// <summary>Loại thuốc</summary>
    [JsonPropertyName("medicationNote")]
    public string? MedicationNote { get; set; }

    /// <summary>Tiền sử phẫu thuật</summary>
    [JsonPropertyName("surgeryHistory")]
    public bool? SurgeryHistory { get; set; }

    /// <summary>Ghi rõ phẫu thuật</summary>
    [JsonPropertyName("surgeryNote")]
    public string? SurgeryNote { get; set; }
}

/// <summary>Tiền sử sản khoa — Para, bảng các lần mang thai trước</summary>
public class ObstetricHistoryDto
{
    /// <summary>Para</summary>
    [JsonPropertyName("para")]
    public int? Para { get; set; }

    /// <summary>Bảng tiền sử các lần mang thai trước</summary>
    [JsonPropertyName("previousPregnancies")]
    public List<PreviousPregnancyDto>? PreviousPregnancies { get; set; }
}

/// <summary>Thông tin 1 lần mang thai trước</summary>
public class PreviousPregnancyDto
{
    /// <summary>Thời gian, nơi kết thúc thai nghén</summary>
    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    /// <summary>Tuổi thai (sảy, sinh non, đủ tháng, già tháng)</summary>
    [JsonPropertyName("gestationalAge")]
    public string? GestationalAge { get; set; }

    /// <summary>Diễn biến khi có thai</summary>
    [JsonPropertyName("complicationsDuringPregnancy")]
    public string? ComplicationsDuringPregnancy { get; set; }

    /// <summary>Cách thức sinh</summary>
    [JsonPropertyName("deliveryMethod")]
    public string? DeliveryMethod { get; set; }

    /// <summary>Trẻ sơ sinh (cân nặng, bệnh tật)</summary>
    [JsonPropertyName("newbornInfo")]
    public string? NewbornInfo { get; set; }

    /// <summary>Hậu sản</summary>
    [JsonPropertyName("postpartum")]
    public string? Postpartum { get; set; }
}

/// <summary>Phụ khoa — chu kỳ kinh, phẫu thuật, khối u</summary>
public class GynecologyHistoryDto
{
    /// <summary>Chu kỳ kinh: "regular" | "irregular"</summary>
    [JsonPropertyName("menstrualCycle")]
    public string? MenstrualCycle { get; set; }

    /// <summary>Số ngày chu kỳ</summary>
    [JsonPropertyName("menstrualCycleDays")]
    public int? MenstrualCycleDays { get; set; }

    /// <summary>Phẫu thuật phụ khoa</summary>
    [JsonPropertyName("gynecologySurgery")]
    public bool? GynecologySurgery { get; set; }

    /// <summary>Ghi rõ phẫu thuật</summary>
    [JsonPropertyName("gynecologySurgeryNote")]
    public string? GynecologySurgeryNote { get; set; }

    /// <summary>Khối u buồng trứng</summary>
    [JsonPropertyName("ovarianTumor")]
    public bool? OvarianTumor { get; set; }

    /// <summary>Khối u tử cung</summary>
    [JsonPropertyName("uterineFibroid")]
    public bool? UterineFibroid { get; set; }

    /// <summary>Dị dạng sinh dục</summary>
    [JsonPropertyName("genitalMalformation")]
    public bool? GenitalMalformation { get; set; }

    /// <summary>Tầng sinh môn</summary>
    [JsonPropertyName("vaginalInfection")]
    public bool? VaginalInfection { get; set; }
}

/// <summary>Tiền sử gia đình — đa thai, dị dạng, bệnh di truyền</summary>
public class FamilyHistoryDto
{
    /// <summary>Có tiền sử gia đình</summary>
    [JsonPropertyName("hasHistory")]
    public bool? HasHistory { get; set; }

    /// <summary>Ghi đầy đủ nội dung</summary>
    [JsonPropertyName("familyHistoryNote")]
    public string? FamilyHistoryNote { get; set; }

    /// <summary>Đa thai</summary>
    [JsonPropertyName("twins")]
    public bool? Twins { get; set; }

    /// <summary>Dị dạng</summary>
    [JsonPropertyName("malformation")]
    public bool? Malformation { get; set; }

    /// <summary>Bệnh di truyền</summary>
    [JsonPropertyName("geneticDisease")]
    public bool? GeneticDisease { get; set; }

    /// <summary>Đái tháo đường</summary>
    [JsonPropertyName("diabetes")]
    public bool? Diabetes { get; set; }

    /// <summary>Tăng huyết áp</summary>
    [JsonPropertyName("hypertension")]
    public bool? Hypertension { get; set; }

    /// <summary>Khác (ghi rõ)</summary>
    [JsonPropertyName("otherNote")]
    public string? OtherNote { get; set; }
}
