using System.Text.Json.Serialization;

namespace FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

/// <summary>
/// Phần A — Thông tin chung của bệnh nhân.
/// FE có thể pre-fill từ UserProfile, user chỉ confirm.
/// </summary>
public class GeneralInfoDto
{
    /// <summary>Cơ sở Khám bệnh, Chữa bệnh</summary>
    [JsonPropertyName("facility")]
    public string? Facility { get; set; }

    /// <summary>Cơ quan chủ quản</summary>
    [JsonPropertyName("managingAuthority")]
    public string? ManagingAuthority { get; set; }

    /// <summary>Số vào viện</summary>
    [JsonPropertyName("admissionNumber")]
    public string? AdmissionNumber { get; set; }

    /// <summary>Mã người bệnh</summary>
    [JsonPropertyName("patientCode")]
    public string? PatientCode { get; set; }

    /// <summary>Họ và tên</summary>
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    /// <summary>Ngày sinh (yyyy-MM-dd)</summary>
    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; set; }

    /// <summary>Tuổi</summary>
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    /// <summary>Điện thoại</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>Nghề nghiệp</summary>
    [JsonPropertyName("occupation")]
    public string? Occupation { get; set; }

    /// <summary>Dân tộc</summary>
    [JsonPropertyName("ethnicity")]
    public string? Ethnicity { get; set; }

    /// <summary>Quốc tịch</summary>
    [JsonPropertyName("nationality")]
    public string? Nationality { get; set; }

    /// <summary>Địa chỉ (số nhà, thôn, phố)</summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>Xã, phường</summary>
    [JsonPropertyName("ward")]
    public string? Ward { get; set; }

    /// <summary>Huyện, Quận</summary>
    [JsonPropertyName("district")]
    public string? District { get; set; }

    /// <summary>Tỉnh, thành phố</summary>
    [JsonPropertyName("province")]
    public string? Province { get; set; }

    /// <summary>Đối tượng: "BHYT" | "thu_phi" | "mien" | "khac"</summary>
    [JsonPropertyName("insuranceType")]
    public string? InsuranceType { get; set; }

    /// <summary>Số thẻ BHYT</summary>
    [JsonPropertyName("insuranceNumber")]
    public string? InsuranceNumber { get; set; }

    /// <summary>Giá trị sử dụng đến (yyyy-MM-dd)</summary>
    [JsonPropertyName("insuranceExpiry")]
    public string? InsuranceExpiry { get; set; }

    /// <summary>Số CCCD / Hộ chiếu / Số định danh cá nhân</summary>
    [JsonPropertyName("idNumber")]
    public string? IdNumber { get; set; }
}
