using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Application.IRepositories;

/// <summary>
/// Generic repository interface for common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public interface IGenericRepository<T> where T : BaseEntity
{
    #region Query Methods

    /// <summary>
    /// Get entity by ID (read-only, no tracking)
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="include">Optional navigation properties to include</param>
    /// <param name="includeDeleted">Include soft-deleted records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity or null if not found</returns>
    Task<T?> GetByIdAsync(
        Guid id,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entity by ID with change tracking (for update/delete operations)
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="include">Optional navigation properties to include</param>
    /// <param name="includeDeleted">Include soft-deleted records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tracked entity or null if not found</returns>
    Task<T?> GetByIdTrackedAsync(
        Guid id,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get single entity matching predicate (read-only, no tracking)
    /// </summary>
    /// <param name="predicate">Filter expression</param>
    /// <param name="include">Optional navigation properties to include</param>
    /// <param name="includeDeleted">Include soft-deleted records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity or null if not found</returns>
    Task<T?> GetSingleAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get single entity matching predicate with change tracking (for update/delete operations)
    /// </summary>
    /// <param name="predicate">Filter expression</param>
    /// <param name="include">Optional navigation properties to include</param>
    /// <param name="includeDeleted">Include soft-deleted records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tracked entity or null if not found</returns>
    Task<T?> GetSingleTrackedAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities matching optional predicate
    /// </summary>
    /// <param name="predicate">Optional filter expression</param>
    /// <param name="include">Optional navigation properties to include</param>
    /// <param name="includeDeleted">Include soft-deleted records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entities</returns>
    Task<IReadOnlyList<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paged results with search, filter, and sort capabilities
    /// </summary>
    /// <param name="options">Query options (page, search, sort, etc.)</param>
    /// <param name="predicate">Optional base filter</param>
    /// <param name="include">Optional navigation properties to include</param>
    /// <param name="searchBuilder">Custom search logic (server-side) applied when Search is provided</param>
    /// <param name="sortMap">Whitelist of sortable fields (LambdaExpression to avoid boxing)</param>
    /// <param name="defaultSort">Default sort expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result</returns>
    Task<PagedResult<T>> GetPagedAsync(
        QueryOptions options,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, string, IQueryable<T>>? searchBuilder = null,
        Dictionary<string, LambdaExpression>? sortMap = null,
        LambdaExpression? defaultSort = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paged results with projection to DTO
    /// </summary>
    /// <typeparam name="TDto">DTO type to project to</typeparam>
    /// <param name="options">Query options</param>
    /// <param name="selector">Projection expression</param>
    /// <param name="predicate">Optional base filter</param>
    /// <param name="include">Optional navigation properties to include</param>
    /// <param name="searchBuilder">Custom search logic</param>
    /// <param name="sortMap">Whitelist of sortable fields (LambdaExpression to avoid boxing)</param>
    /// <param name="defaultSort">Default sort expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of DTOs</returns>
    Task<PagedResult<TDto>> GetPagedAsync<TDto>(
        QueryOptions options,
        Expression<Func<T, TDto>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, string, IQueryable<T>>? searchBuilder = null,
        Dictionary<string, LambdaExpression>? sortMap = null,
        LambdaExpression? defaultSort = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if any entity matches the predicate
    /// </summary>
    /// <param name="predicate">Filter expression</param>
    /// <param name="includeDeleted">Include soft-deleted records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count entities matching optional predicate
    /// </summary>
    /// <param name="predicate">Optional filter expression</param>
    /// <param name="includeDeleted">Include soft-deleted records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of entities</returns>
    Task<long> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    #endregion

    #region Command Methods

    /// <summary>
    /// Add new entity
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple entities
    /// </summary>
    /// <param name="entities">Entities to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing entity
    /// </summary>
    /// <param name="entity">Entity to update</param>
    void Update(T entity);

    /// <summary>
    /// Update multiple entities
    /// </summary>
    /// <param name="entities">Entities to update</param>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Hard delete entity
    /// </summary>
    /// <param name="entity">Entity to delete</param>
    void Delete(T entity);

    /// <summary>
    /// Hard delete multiple entities
    /// </summary>
    /// <param name="entities">Entities to delete</param>
    void DeleteRange(IEnumerable<T> entities);

    /// <summary>
    /// Soft delete entity (set DeletedAt)
    /// </summary>
    /// <param name="entity">Entity to soft delete</param>
    Task SoftDeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete entity by ID
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore soft-deleted entity
    /// </summary>
    /// <param name="entity">Entity to restore</param>
    Task RestoreAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore soft-deleted entity by ID
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion
}
