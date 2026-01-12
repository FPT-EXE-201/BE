using System.Linq.Expressions;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.Languages;

/// <summary>
/// Query specification for Language entity listing
/// </summary>
public static class LanguageListQuerySpec
{
    /// <summary>
    /// Search whitelist: key -> string field expression
    /// </summary>
    public static readonly Dictionary<string, Expression<Func<Language, string?>>> SearchMap = new()
    {
        ["code"] = l => l.Code,
        ["name"] = l => l.Name
    };

    /// <summary>
    /// Default fields to search when SearchIn is not specified
    /// </summary>
    public static readonly string[] DefaultSearchKeys = ["code", "name"];

    /// <summary>
    /// Sort whitelist: key -> sort expression (LambdaExpression to avoid boxing)
    /// </summary>
    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["code"] = (Expression<Func<Language, string>>)(l => l.Code),
        ["name"] = (Expression<Func<Language, string>>)(l => l.Name),
        ["createdat"] = (Expression<Func<Language, DateTime>>)(l => l.CreatedAt),
        ["isactive"] = (Expression<Func<Language, bool>>)(l => l.IsActive)
    };

    /// <summary>
    /// Default sort when SortBy is not specified
    /// </summary>
    public static readonly LambdaExpression DefaultSort = (Expression<Func<Language, string>>)(l => l.Name);

    /// <summary>
    /// Projection selector to DTO
    /// </summary>
    public static readonly Expression<Func<Language, LanguageListDto>> Selector = l => new LanguageListDto
    {
        Code = l.Code,
        Name = l.Name,
        IsActive = l.IsActive,
        CreatedAt = l.CreatedAt
    };
}

/// <summary>
/// DTO for Language listing
/// </summary>
public class LanguageListDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
