using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealItemRepository : GenericRepository<MealItem>, IMealItemRepository
{
    public MealItemRepository(AppDbContext context) : base(context) { }
}
