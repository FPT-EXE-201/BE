using System.Linq.Expressions;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.Users;

/// <summary>
/// Query specification for User entity listing
/// </summary>
public static class UserListQuerySpec
{
    /// <summary>
    /// Search whitelist: key -> string field expression
    /// </summary>
    public static readonly Dictionary<string, Expression<Func<User, string?>>> SearchMap = new()
    {
        ["email"] = u => u.Email,
        ["phone"] = u => u.Phone,
        ["fullname"] = u => u.Profile != null ? u.Profile.FullName : null
    };

    /// <summary>
    /// Default fields to search when SearchIn is not specified
    /// </summary>
    public static readonly string[] DefaultSearchKeys = ["email", "phone", "fullname"];

    /// <summary>
    /// Sort whitelist: key -> sort expression (LambdaExpression to avoid boxing)
    /// </summary>
    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["email"] = (Expression<Func<User, string?>>)(u => u.Email),
        ["phone"] = (Expression<Func<User, string?>>)(u => u.Phone),
        ["status"] = (Expression<Func<User, string>>)(u => u.Status.ToString()),
        ["createdat"] = (Expression<Func<User, DateTime>>)(u => u.CreatedAt),
        ["lastloginat"] = (Expression<Func<User, DateTime?>>)(u => u.LastLoginAt)
    };

    /// <summary>
    /// Default sort when SortBy is not specified
    /// </summary>
    public static readonly LambdaExpression DefaultSort = (Expression<Func<User, DateTime>>)(u => u.CreatedAt);

    /// <summary>
    /// Projection selector to DTO
    /// </summary>
    public static readonly Expression<Func<User, UserListDto>> Selector = u => new UserListDto
    {
        Id = u.Id,
        Email = u.Email,
        Phone = u.Phone,
        FullName = u.Profile != null ? u.Profile.FullName : null,
        AvatarUrl = u.Profile != null ? u.Profile.AvatarUrl : null,
        Status = u.Status.ToString(),
        IsEmailVerified = u.IsEmailVerified,
        IsPhoneVerified = u.IsPhoneVerified,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt
    };
}

/// <summary>
/// DTO for User listing
/// </summary>
public class UserListDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = default!;
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
