using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMedicalDocumentRepository : IGenericRepository<MedicalDocument>
{
    /// <summary>List documents theo pregnancy, include StorageFile + DocumentType. Optional isFavorite filter.</summary>
    Task<List<MedicalDocument>> GetByPregnancyIdWithDetailsAsync(
        Guid pregnancyId, bool? isFavorite = null, CancellationToken cancellationToken = default);

    /// <summary>Lấy 1 document với toàn bộ details (StorageFile, DocumentType, OCR, Pregnancy).</summary>
    Task<MedicalDocument?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken cancellationToken = default);
}
