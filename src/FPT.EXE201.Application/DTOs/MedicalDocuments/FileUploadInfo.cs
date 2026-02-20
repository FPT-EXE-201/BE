namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

/// <summary>
/// Thông tin 1 file cần upload (dùng trong service layer).
/// </summary>
public record FileUploadInfo(
    Stream Stream,
    string FileName,
    string ContentType,
    long FileSize
);
