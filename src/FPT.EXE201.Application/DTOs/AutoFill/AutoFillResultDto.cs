namespace FPT.EXE201.Application.DTOs.AutoFill;

/// <summary>
/// Response sau khi confirm: tóm tắt entities đã được tạo.
/// </summary>
public class AutoFillResultDto
{
    /// <summary>OcrResult ID đã confirm.</summary>
    public Guid OcrResultId { get; set; }

    /// <summary>Document ID nguồn (để FE navigate xem ảnh gốc).</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Document type đã dùng để auto-fill.</summary>
    public string DocumentTypeCode { get; set; } = "";

    /// <summary>Visit ID đã tạo hoặc đã link (cho PRENATAL_CHECKUP). Null nếu không tạo visit.</summary>
    public Guid? CreatedVisitId { get; set; }

    /// <summary>Danh sách Test IDs đã tạo (cho BLOOD_TEST, URINE_TEST, ULTRASOUND).</summary>
    public List<Guid> CreatedTestIds { get; set; } = new();

    /// <summary>Document đã được auto-link vào visit hay chưa.</summary>
    public bool DocumentLinkedToVisit { get; set; }

    /// <summary>Tóm tắt cho user (hiển thị snackbar/toast).</summary>
    public string Summary { get; set; } = "";
}
