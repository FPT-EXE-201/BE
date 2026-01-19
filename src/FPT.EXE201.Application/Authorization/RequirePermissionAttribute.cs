using Microsoft.AspNetCore.Authorization;

namespace FPT.EXE201.Application.Authorization
{
    /// <summary>
    /// Attribute for permission-based authorization
    /// Usage: [RequirePermission("users.read.any")]
    /// </summary>
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "Permission:";

        public RequirePermissionAttribute(string permission)
        {
            Policy = $"{PolicyPrefix}{permission}";
        }
    }
}
