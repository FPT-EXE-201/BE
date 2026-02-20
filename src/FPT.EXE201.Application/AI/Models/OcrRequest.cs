namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Request gửi tới OCR provider.
/// </summary>
public record OcrRequest(
    Stream FileStream,
    string FileName,
    string ContentType,
    string? LanguageHint = "vi"
);
