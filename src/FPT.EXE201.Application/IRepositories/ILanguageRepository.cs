using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    public interface ILanguageRepository
    {
        Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default);
        Task<Language?> GetByCodeAsync(string code, CancellationToken ct = default); // “VI”, “EN”
        Task<Language?> GetDefaultAsync(CancellationToken ct = default);
    }
}
