namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Trạng thái pipeline OCR + AI Extraction.
/// Flow: Pending → OcrProcessing → OcrCompleted → AiExtracting → Succeeded
/// Bất kỳ bước nào fail → Failed
///
/// ⚠️ MIGRATION NOTE: Week 4 dùng "Processing" → Week 5 rename thành "OcrProcessing".
///    Nếu DB đã có rows với status = "Processing", cần chạy SQL update trước:
///    UPDATE ocr_results SET status = 'OcrProcessing' WHERE status = 'Processing';
/// </summary>
public enum OcrStatus
{
    /// <summary>Đang chờ xử lý.</summary>
    Pending,

    /// <summary>Azure Document Intelligence đang chạy OCR.</summary>
    OcrProcessing,

    /// <summary>OCR hoàn tất, raw text đã có. Chờ AI extraction.</summary>
    OcrCompleted,

    /// <summary>Gemini đang trích xuất structured data từ raw text.</summary>
    AiExtracting,

    /// <summary>Pipeline hoàn tất: cả OCR + AI extraction thành công.</summary>
    Succeeded,

    /// <summary>Pipeline thất bại ở bất kỳ bước nào.</summary>
    Failed,

    /// <summary>WEEK 5.5: User đã review + confirm extracted data → entities created.</summary>
    Confirmed
}
