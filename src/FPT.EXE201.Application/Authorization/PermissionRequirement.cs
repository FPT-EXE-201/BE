using Microsoft.AspNetCore.Authorization;

namespace FPT.EXE201.Application.Authorization
{
    /// <summary>
    /// Permission-based authorization requirement
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
