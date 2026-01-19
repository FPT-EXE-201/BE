using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FPT.EXE201.Application.Authorization
{
    /// <summary>
    /// Handler for permission-based authorization (Approach 2 - Read from JWT claims, no DB query)
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Approach 2: Read permissions from JWT claims - NO DATABASE QUERY
            // Permissions were added to claims during login by AuthService
            var userPermissions = context.User.FindAll("permissions")
                .Select(c => c.Value)
                .ToList();

            // Check if required permission exists in user's claims
            if (userPermissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }

            // Return completed task (synchronous, no DB query)
            return Task.CompletedTask;
        }
    }
}
