using System.Linq.Expressions;
using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation with EF Core
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    #region Query Methods

    public async Task<T?> GetByIdAsync(
        Guid id,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = includeDeleted
            ? _dbSet.IgnoreQueryFilters().AsNoTracking()
            : _dbSet.AsNoTracking();

        if (include != null)
            query = include(query);

        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<T?> GetByIdTrackedAsync(
        Guid id,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = includeDeleted
            ? _dbSet.IgnoreQueryFilters().AsQueryable()
            : _dbSet.AsQueryable(); // WITH tracking for update/delete

        if (include != null)
            query = include(query);

        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<T?> GetSingleAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = includeDeleted
            ? _dbSet.IgnoreQueryFilters().AsNoTracking()
            : _dbSet.AsNoTracking();

        if (include != null)
            query = include(query);

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<T?> GetSingleTrackedAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = includeDeleted
            ? _dbSet.IgnoreQueryFilters().AsQueryable()
            : _dbSet.AsQueryable(); // WITH tracking for update/delete

        if (include != null)
            query = include(query);

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = includeDeleted
            ? _dbSet.IgnoreQueryFilters().AsNoTracking()
            : _dbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        if (include != null)
            query = include(query);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<T>> GetPagedAsync(
        QueryOptions options,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, string, IQueryable<T>>? searchBuilder = null,
        Dictionary<string, LambdaExpression>? sortMap = null,
        LambdaExpression? defaultSort = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = options.IncludeDeleted
            ? _dbSet.IgnoreQueryFilters().AsNoTracking()
            : _dbSet.AsNoTracking();

        // Apply base predicate
        if (predicate != null)
            query = query.Where(predicate);

        // Apply custom search logic (server-side, SQL-translatable)
        if (!string.IsNullOrWhiteSpace(options.Search) && searchBuilder != null)
        {
            query = searchBuilder(query, options.Search.Trim());
        }

        // Get total count before pagination
        var totalItems = await query.LongCountAsync(cancellationToken);

        // Apply sorting
        query = SortHelper.ApplySort(query, options, sortMap, defaultSort);

        // Apply includes after sorting but before pagination
        if (include != null)
            query = include(query);

        // Apply pagination
        var items = await query
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, options.Page, options.PageSize, totalItems);
    }

    public async Task<PagedResult<TDto>> GetPagedAsync<TDto>(
        QueryOptions options,
        Expression<Func<T, TDto>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, string, IQueryable<T>>? searchBuilder = null,
        Dictionary<string, LambdaExpression>? sortMap = null,
        LambdaExpression? defaultSort = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = options.IncludeDeleted
            ? _dbSet.IgnoreQueryFilters().AsNoTracking()
            : _dbSet.AsNoTracking();

        // Apply base predicate
        if (predicate != null)
            query = query.Where(predicate);

        // Apply custom search logic
        if (!string.IsNullOrWhiteSpace(options.Search) && searchBuilder != null)
        {
            query = searchBuilder(query, options.Search.Trim());
        }

        // Get total count before pagination
        var totalItems = await query.LongCountAsync(cancellationToken);

        // Apply sorting
        query = SortHelper.ApplySort(query, options, sortMap, defaultSort);

        // Apply includes after sorting but before pagination
        if (include != null)
            query = include(query);

        // Apply pagination and projection
        var items = await query
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new PagedResult<TDto>(items, options.Page, options.PageSize, totalItems);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = includeDeleted
            ? _dbSet.IgnoreQueryFilters().AsNoTracking()
            : _dbSet.AsNoTracking();

        return await query.AnyAsync(predicate, cancellationToken);
    }

    public async Task<long> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = includeDeleted
            ? _dbSet.IgnoreQueryFilters().AsNoTracking()
            : _dbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.LongCountAsync(cancellationToken);
    }

    #endregion

    #region Command Methods

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
        }
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.UpdatedAt = now;
        }
        _dbSet.UpdateRange(entities);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void DeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public async Task SoftDeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity != null)
        {
            await SoftDeleteAsync(entity, cancellationToken);
        }
    }

    public async Task RestoreAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.DeletedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        
        if (entity != null)
        {
            await RestoreAsync(entity, cancellationToken);
        }
    }

    #endregion
}
