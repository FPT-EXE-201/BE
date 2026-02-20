using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IRefDocumentTypeRepository : IGenericRepository<RefDocumentType>
{
    /// <summary>Lấy tất cả document types đang active, include translations.</summary>
    Task<List<RefDocumentType>> GetActiveWithTranslationsAsync(string langCode, CancellationToken cancellationToken = default);
}
