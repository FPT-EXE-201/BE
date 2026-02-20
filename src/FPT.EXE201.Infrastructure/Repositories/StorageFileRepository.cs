using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class StorageFileRepository : GenericRepository<StorageFile>, IStorageFileRepository
{
    public StorageFileRepository(AppDbContext context) : base(context) { }
}
