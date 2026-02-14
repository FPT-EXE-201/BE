using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.Languages;

/// <summary>
/// Query specification for Language entity listing
/// </summary>
public static class LanguageListQuerySpec
{
    public static readonly Dictionary<string, Expression<Func<Language, string?>>> SearchMap = new()
    {
        ["code"] = l => l.Code,
        ["name"] = l => l.Name
    };

    public static readonly string[] DefaultSearchKeys = ["code", "name"];

    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["code"] = (Expression<Func<Language, string>>)(l => l.Code),
        ["name"] = (Expression<Func<Language, string>>)(l => l.Name),
        ["createdat"] = (Expression<Func<Language, DateTime>>)(l => l.CreatedAt),
        ["isactive"] = (Expression<Func<Language, bool>>)(l => l.IsActive)
    };

    public static readonly LambdaExpression DefaultSort = (Expression<Func<Language, string>>)(l => l.Name);

    /// <summary>Metadata for FE: GET /api/ref/query-specs</summary>
    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "name",
        DefaultSortDir = "asc"
    };

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
