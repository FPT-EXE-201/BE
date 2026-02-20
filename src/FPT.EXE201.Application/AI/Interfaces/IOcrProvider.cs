using FPT.EXE201.Application.AI.Models;

namespace FPT.EXE201.Application.AI.Interfaces;

/// <summary>
/// Abstraction cho OCR provider (Azure Document Intelligence, Google Vision, Tesseract, etc.).
/// </summary>
public interface IOcrProvider
{
    /// <summary>Chạy OCR trên file, trả raw text.</summary>
    Task<OcrResponse> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken = default);

    /// <summary>Danh sách file types mà provider hỗ trợ.</summary>
    IReadOnlyList<string> SupportedContentTypes { get; }
}
