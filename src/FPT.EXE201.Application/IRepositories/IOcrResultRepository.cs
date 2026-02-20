using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IOcrResultRepository : IGenericRepository<OcrResult>
{
    /// <summary>Lấy OCR result mới nhất của document.</summary>
    Task<OcrResult?> GetLatestByDocumentIdAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Get all OCR results for a document, ordered by run number descending.</summary>
    Task<List<OcrResult>> GetByDocumentIdAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Lấy danh sách OCR đang pending để xử lý batch.</summary>
    Task<List<OcrResult>> GetPendingAsync(
        int limit = 10, CancellationToken cancellationToken = default);
}
