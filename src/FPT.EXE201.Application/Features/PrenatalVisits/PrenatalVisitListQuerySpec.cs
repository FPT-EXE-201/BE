using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.PrenatalVisits;

/// <summary>
/// Query specification for PrenatalVisit entity listing.
/// Searchable: notes, location | Sortable: visitDate, location, createdAt
/// </summary>
public static class PrenatalVisitListQuerySpec
{
    public static readonly Dictionary<string, Expression<Func<PrenatalVisit, string?>>> SearchMap = new()
    {
        ["notes"] = v => v.Notes,
        ["location"] = v => v.Location
    };

    public static readonly string[] DefaultSearchKeys = ["notes", "location"];

    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["visitdate"] = (Expression<Func<PrenatalVisit, DateOnly>>)(v => v.VisitDate),
        ["location"] = (Expression<Func<PrenatalVisit, string?>>)(v => v.Location),
        ["createdat"] = (Expression<Func<PrenatalVisit, DateTime>>)(v => v.CreatedAt)
    };

    public static readonly LambdaExpression DefaultSort =
        (Expression<Func<PrenatalVisit, DateOnly>>)(v => v.VisitDate);

    /// <summary>Metadata for FE: GET /api/ref/query-specs</summary>
    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "visitdate",
        DefaultSortDir = "desc"
    };
}
