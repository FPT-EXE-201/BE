namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

/// <summary>
/// Response trả về thông tin tài liệu y tế + file info.
/// </summary>
public class MedicalDocumentDto
{
    public Guid Id { get; set; }
    public Guid PregnancyId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid? DocumentTypeId { get; set; }

    /// <summary>Tên loại tài liệu theo ngôn ngữ. Ví dụ: "Phiếu khám thai".</summary>
    public string? DocumentTypeDisplayName { get; set; }

    /// <summary>
    /// Danh sách files đã upload (ordered by SortOrder).
    /// Hỗ trợ multi-file: 1 document có thể chứa nhiều ảnh.
    /// </summary>
    public List<DocumentFileDto> Files { get; set; } = new();

    /// <summary>Tổng kích thước tất cả files (bytes).</summary>
    public long TotalFileSizeBytes { get; set; }

    public string? Title { get; set; }
    public DateOnly? DocumentDate { get; set; }
    public DateTime CapturedAt { get; set; }

    /// <summary>Nguồn gốc: "Upload", "Share", "Import".</summary>
    public string Source { get; set; } = null!;

    public string? Notes { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
