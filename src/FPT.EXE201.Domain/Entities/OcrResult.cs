using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Kết quả một lần chạy OCR trên tài liệu y tế.
/// Mỗi document có thể chạy OCR nhiều lần (user retry khi kết quả sai).
/// 
/// Flow:
///   1. Upload ảnh → status = Pending
///   2. OCR engine xử lý → status = Processing
///   3. Thành công → status = Succeeded, lưu raw_text + structured_json
///      Thất bại → status = Failed, lưu error_message
///   4. structured_json chứa output từ Gemini AI (visit + test data parsed)
/// 
/// StructuredJson chứa PrenatalExaminationData đầy đủ theo mẫu phiếu khám thai:
///   - GeneralInfo (tên, ngày sinh, nhóm máu, BHYT...)
///   - PhysicalExamination.VitalSigns (mạch, HA, cân nặng)
///   - LabTests (máu, nước tiểu, siêu âm)
///   - Diagnosis, TreatmentPlan, NextVisit...
///
/// → Là SINGLE SOURCE OF TRUTH cho dữ liệu trích xuất từ ảnh.
///   MedicalDocument KHÔNG có MetadataJson (đã loại bỏ để tránh trùng lặp).
///   Khi cần structured data → query OcrResult mới nhất (max OcrRunNumber).
/// </summary>
public class OcrResult : BaseEntity
{
    /// <summary>FK → MedicalDocument. Tài liệu được chạy OCR.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Số lần chạy OCR (1, 2, 3...). Tăng mỗi lần user rerun.
    /// </summary>
    public int OcrRunNumber { get; set; }

    /// <summary>Trạng thái: Pending → Processing → Succeeded / Failed.</summary>
    public OcrStatus Status { get; set; }

    /// <summary>
    /// Tên OCR engine đã dùng. Ví dụ: "google-vision", "tesseract", "stub-v1".
    /// </summary>
    public string? OcrEngine { get; set; }

    /// <summary>
    /// Gợi ý ngôn ngữ cho OCR. Ví dụ: "vi" cho tiếng Việt.
    /// </summary>
    public string? LanguageHint { get; set; }

    /// <summary>
    /// Văn bản thô trích xuất từ ảnh (raw OCR output, chưa parse).
    /// </summary>
    public string? RawText { get; set; }

    /// <summary>
    /// JSON có cấu trúc PrenatalExaminationData sau khi Gemini AI parse raw text.
    /// Đây là SINGLE SOURCE OF TRUTH — MedicalDocument không giữ bản copy.
    /// Schema: GeneralInfo, PhysicalExamination, LabTests, Diagnosis, TreatmentPlan...
    /// Khi AI service tạo PrenatalVisit, nó extract VitalsJson từ đây.
    /// </summary>
    public string? StructuredJson { get; set; }

    /// <summary>
    /// Độ tin cậy của kết quả OCR (0.00 - 100.00).
    /// </summary>
    public decimal? ConfidenceScore { get; set; }

    /// <summary>
    /// Thông báo lỗi nếu OCR thất bại.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // ═══ Week 5: AI Processing Fields ═══

    /// <summary>Thời gian OCR (Azure) xử lý, tính bằng ms.</summary>
    public int? OcrProcessingTimeMs { get; set; }

    /// <summary>Tên model AI đã sử dụng (e.g., "gemini-2.5-flash").</summary>
    public string? AiModelUsed { get; set; }

    /// <summary>Số tokens AI đã sử dụng (prompt + completion).</summary>
    public int? AiTokensUsed { get; set; }

    /// <summary>Thời gian AI extraction xử lý, tính bằng ms.</summary>
    public int? AiProcessingTimeMs { get; set; }

    /// <summary>FK → AiPromptTemplate. Template đã dùng để tạo prompt.</summary>
    public Guid? AiPromptTemplateId { get; set; }

    // ═══ WEEK 5.5: Confirm & Auto-Fill Fields ═══

    /// <summary>Thời điểm user confirm extracted data.</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>User ID đã confirm.</summary>
    public Guid? ConfirmedBy { get; set; }

    /// <summary>JSON dữ liệu user đã review + chỉnh sửa (có thể khác StructuredJson).</summary>
    public string? ConfirmedJson { get; set; }

    /// <summary>JSON kết quả auto-fill: {"visitId":"...","testIds":["..."],"summary":"..."}.</summary>
    public string? AutoFillResultJson { get; set; }

    // ═══ Navigation ═══

    /// <summary>Tài liệu được chạy OCR.</summary>
    public MedicalDocument Document { get; set; } = null!;

    /// <summary>Template AI đã sử dụng cho extraction.</summary>
    public AiPromptTemplate? AiPromptTemplate { get; set; }
}
