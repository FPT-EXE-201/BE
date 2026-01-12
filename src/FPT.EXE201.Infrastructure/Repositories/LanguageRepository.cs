using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories
{
    public class LanguageRepository : ILanguageRepository
    {
        private readonly AppDbContext _context;

        public LanguageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default)
        {
            return await _context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync(ct);
        }

        public async Task<Language?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            return await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Code == code, ct);
        }

        public async Task<Language?> GetDefaultAsync(CancellationToken ct = default)
        {
            // Default to Vietnamese ("vi") or first active language
            var defaultLang = await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Code == "vi" && l.IsActive, ct);

            if (defaultLang == null)
            {
                defaultLang = await _context.Languages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.IsActive, ct);
            }

            return defaultLang;
        }
    }
}
