using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.MealPlans;

public static class MealPlanListQuerySpec
{
    public static readonly Dictionary<string, Expression<Func<MealPlan, string?>>> SearchMap = new()
    {
        ["title"] = m => m.Title,
        ["notes"] = m => m.Notes
    };
    public static readonly string[] DefaultSearchKeys = ["title"];

    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["startdate"] = (Expression<Func<MealPlan, DateOnly>>)(m => m.StartDate),
        ["enddate"]   = (Expression<Func<MealPlan, DateOnly>>)(m => m.EndDate),
        ["createdat"] = (Expression<Func<MealPlan, DateTime>>)(m => m.CreatedAt)
    };
    public static readonly LambdaExpression DefaultSort =
        (Expression<Func<MealPlan, DateTime>>)(m => m.CreatedAt);

    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "createdat",
        DefaultSortDir = "desc"
    };
}
