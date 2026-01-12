using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;

namespace FPT.EXE201.Application.Common.Querying;

/// <summary>
/// Helper for building EF-translatable sort expressions without boxing
/// </summary>
public static class SortHelper
{
    /// <summary>
    /// Apply sorting to query using whitelist and options (no boxing with LambdaExpression)
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">Source query</param>
    /// <param name="options">Query options with SortBy and SortDir</param>
    /// <param name="sortMap">Whitelist: key -> LambdaExpression (preserves actual type)</param>
    /// <param name="defaultSort">Default sort expression when SortBy is not specified</param>
    /// <param name="defaultSortByCreatedAt">Fallback to CreatedAt if defaultSort is null (only for BaseEntity)</param>
    /// <returns>Sorted query (EF-translatable)</returns>
    public static IQueryable<T> ApplySort<T>(
        IQueryable<T> query,
        QueryOptions options,
        Dictionary<string, LambdaExpression>? sortMap,
        LambdaExpression? defaultSort,
        bool defaultSortByCreatedAt = true)
    {
        LambdaExpression? sortExpression = null;

        // Try to get sort from whitelist map
        if (!string.IsNullOrWhiteSpace(options.SortBy) && sortMap != null)
        {
            var sortKey = options.SortBy.Trim().ToLowerInvariant();
            if (sortMap.TryGetValue(sortKey, out var expr))
            {
                sortExpression = expr;
            }
        }

        // Fall back to default sort or CreatedAt (if applicable)
        if (sortExpression == null)
        {
            sortExpression = defaultSort;
            
            // Only use CreatedAt fallback if explicitly enabled and entity has it
            if (sortExpression == null && defaultSortByCreatedAt)
            {
                // This assumes T has CreatedAt (BaseEntity)
                // Cast to dynamic expression - will work for BaseEntity types
                sortExpression = Expression.Lambda(
                    Expression.Property(
                        Expression.Parameter(typeof(T), "e"),
                        "CreatedAt"),
                    Expression.Parameter(typeof(T), "e"));
            }
        }

        // If still null, return unsorted
        if (sortExpression == null)
            return query;

        // Apply sorting direction using reflection to avoid boxing
        return ApplyOrderDynamic(query, sortExpression, options.IsAscending);
    }

    /// <summary>
    /// Apply OrderBy/OrderByDescending dynamically with correct generic type (no boxing)
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="source">Source query</param>
    /// <param name="keySelector">Sort key selector (LambdaExpression preserves actual return type)</param>
    /// <param name="ascending">True for ascending, false for descending</param>
    /// <returns>Ordered query</returns>
    public static IQueryable<T> ApplyOrderDynamic<T>(
        IQueryable<T> source,
        LambdaExpression keySelector,
        bool ascending)
    {
        var methodName = ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending);

        // Get OrderBy/OrderByDescending method with 2 parameters
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == methodName && m.GetParameters().Length == 2);

        // Make generic method: OrderBy<T, TKey> where TKey = keySelector.ReturnType
        var genericMethod = method.MakeGenericMethod(typeof(T), keySelector.ReturnType);

        // Invoke: Queryable.OrderBy(source, keySelector)
        return (IQueryable<T>)genericMethod.Invoke(null, new object[] { source, keySelector })!;
    }
}
