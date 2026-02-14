namespace FPT.EXE201.Application.DTOs.Common;

/// <summary>
/// Query options for pagination, search, filtering, and sorting
/// </summary>
public class QueryOptions
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>
    /// Page number (1-based, default: 1)
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page (default: 20, max: 100)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => value
        };
    }

    /// <summary>
    /// Search term for filtering results
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// CSV of field keys to search in (e.g., "email,phone,fullName").
    /// If empty/null, uses default search fields defined in QuerySpec.
    /// </summary>
    public string? SearchIn { get; set; }

    /// <summary>
    /// Field name to sort by (must be whitelisted in repository)
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction: "asc" or "desc" (default: "desc")
    /// </summary>
    public string SortDir { get; set; } = "desc";


    /// <summary>
    /// Include soft-deleted records in results
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Indicates if sorting should be ascending
    /// </summary>
    public bool IsAscending => SortDir?.ToLower() == "asc";
}
