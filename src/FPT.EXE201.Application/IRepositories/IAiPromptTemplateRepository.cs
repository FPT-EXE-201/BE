using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IAiPromptTemplateRepository : IGenericRepository<AiPromptTemplate>
{
    /// <summary>
    /// Lấy active prompt template theo key (latest version).
    /// Ví dụ: GetActiveByKeyAsync("medical_record.extraction")
    /// </summary>
    Task<AiPromptTemplate?> GetActiveByKeyAsync(string templateKey, CancellationToken cancellationToken = default);
}
