namespace FPT.EXE201.Application.DTOs.Common;

/// <summary>
/// Metadata describing the search/sort capabilities of a paged endpoint.
/// FE gọi GET /api/ref/query-specs → dùng để render dynamic UI (checkbox, dropdown).
/// </summary>
public record QuerySpecMetadataDto
{
    /// <summary>Fields that can be searched (used with SearchIn parameter)</summary>
    public required IReadOnlyList<string> SearchableFields { get; init; }

    /// <summary>Fields searched by default when SearchIn is omitted</summary>
    public required IReadOnlyList<string> DefaultSearchFields { get; init; }

    /// <summary>Fields that can be sorted (used with SortBy parameter)</summary>
    public required IReadOnlyList<string> SortableFields { get; init; }

    /// <summary>Default sort field when SortBy is omitted</summary>
    public required string DefaultSortBy { get; init; }

    /// <summary>Default sort direction: "desc" or "asc"</summary>
    public string DefaultSortDir { get; init; } = "desc";
}
