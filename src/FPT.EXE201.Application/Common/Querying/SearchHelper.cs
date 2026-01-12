using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;

namespace FPT.EXE201.Application.Common.Querying;

/// <summary>
/// Helper for building EF-translatable search expressions
/// </summary>
public static class SearchHelper
{
    /// <summary>
    /// Parse CSV string into distinct lowercase keys
    /// </summary>
    public static string[] ParseKeys(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<string>();

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(k => k.ToLowerInvariant())
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Build OR predicate: (field1 != null && field1.Contains(term)) OR (field2 != null && field2.Contains(term)) ...
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="searchTerm">The search term</param>
    /// <param name="fieldExpressions">Field expressions to search in</param>
    /// <returns>Combined OR expression, or null if no fields</returns>
    public static Expression<Func<T, bool>>? BuildContainsOrPredicate<T>(
        string searchTerm,
        IEnumerable<Expression<Func<T, string?>>> fieldExpressions)
    {
        var fields = fieldExpressions.ToList();
        if (fields.Count == 0 || string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var parameter = Expression.Parameter(typeof(T), "e");
        Expression? combinedOr = null;

        // Cache method and constant outside loop for performance
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        var termConst = Expression.Constant(searchTerm);

        foreach (var fieldExpr in fields)
        {
            // Replace original parameter with our unified parameter
            var body = new ReplaceExpressionVisitor(fieldExpr.Parameters[0], parameter)
                .Visit(fieldExpr.Body)!;

            // field != null (use body.Type for proper nullable handling)
            var notNull = Expression.NotEqual(body, Expression.Constant(null, body.Type));

            // field.Contains(searchTerm) - MySQL collation handles case-insensitivity
            var contains = Expression.Call(body, containsMethod, termConst);

            // (field != null && field.Contains(term))
            var fieldPredicate = Expression.AndAlso(notNull, contains);

            // OR with previous
            combinedOr = combinedOr == null
                ? fieldPredicate
                : Expression.OrElse(combinedOr, fieldPredicate);
        }

        return combinedOr == null
            ? null
            : Expression.Lambda<Func<T, bool>>(combinedOr, parameter);
    }

    /// <summary>
    /// Apply search filter to query using whitelist and options
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">Source query</param>
    /// <param name="options">Query options with Search and SearchIn</param>
    /// <param name="searchMap">Whitelist: key -> field expression</param>
    /// <param name="defaultKeys">Default keys when SearchIn is empty</param>
    /// <returns>Filtered query (EF-translatable)</returns>
    public static IQueryable<T> ApplySearch<T>(
        IQueryable<T> query,
        QueryOptions options,
        Dictionary<string, Expression<Func<T, string?>>> searchMap,
        string[] defaultKeys)
    {
        if (string.IsNullOrWhiteSpace(options.Search))
            return query;

        return ApplySearchCore(query, options.Search.Trim(), options.SearchIn, searchMap, defaultKeys);
    }

    /// <summary>
    /// Create a searchBuilder delegate for use with GenericRepository.GetPagedAsync
    /// </summary>
    public static Func<IQueryable<T>, string, IQueryable<T>> CreateSearchBuilder<T>(
        Dictionary<string, Expression<Func<T, string?>>> searchMap,
        string[] defaultKeys,
        QueryOptions options)
    {
        return (query, searchTerm) => ApplySearchCore(query, searchTerm, options.SearchIn, searchMap, defaultKeys);
    }   

    /// <summary>
    /// Core search logic: parse fields, build predicate, apply to query
    /// </summary>
    private static IQueryable<T> ApplySearchCore<T>(
        IQueryable<T> query,
        string searchTerm,
        string? searchInCsv,
        Dictionary<string, Expression<Func<T, string?>>> searchMap,
        string[] defaultKeys)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        // Parse requested keys or use defaults
        var requestedKeys = ParseKeys(searchInCsv);
        var keysToUse = requestedKeys.Length > 0 ? requestedKeys : defaultKeys;

        // Filter to only whitelisted keys using TryGetValue (single lookup)
        var validFields = new List<Expression<Func<T, string?>>>();
        foreach (var key in keysToUse)
        {
            if (searchMap.TryGetValue(key, out var expr))
                validFields.Add(expr);
        }

        if (validFields.Count == 0)
            return query;

        var predicate = BuildContainsOrPredicate(searchTerm, validFields);
        return predicate == null ? query : query.Where(predicate);
    }

    /// <summary>
    /// Expression visitor to replace all matching nodes in Expression Tree
    /// </summary>
    private sealed class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
            => node == _oldValue ? _newValue : base.Visit(node);
    }
}
