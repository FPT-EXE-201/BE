using System.Security.Claims;
using FPT.EXE201.Api.Controllers;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.RBAC;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers
{
    [Route("api/user-roles")]
    [Authorize]
    public class UserRolesController : BaseApiController
    {
        private readonly IUserRoleService _userRoleService;

        public UserRolesController(IUserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }

        /// <summary>
        /// Get current user's roles
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyRoles()
        {
            var userId = GetCurrentUserId();
            var roles = await _userRoleService.GetUserRolesAsync(userId);
            return Success(roles);
        }

        /// <summary>
        /// Get current user's permissions
        /// </summary>
        [HttpGet("me/permissions")]
        public async Task<IActionResult> GetMyPermissions()
        {
            var userId = GetCurrentUserId();
            var permissions = await _userRoleService.GetUserPermissionsAsync(userId);
            return Success(permissions);
        }

        /// <summary>
        /// Get user roles (Admin only)
        /// </summary>
        [HttpGet("users/{userId}")]
        [RequirePermission("rbac.roles.read")]
        public async Task<IActionResult> GetUserRoles(Guid userId)
        {
            var roles = await _userRoleService.GetUserRolesAsync(userId);
            return Success(roles);
        }

        /// <summary>
        /// Get user permissions (Admin only)
        /// </summary>
        [HttpGet("users/{userId}/permissions")]
        [RequirePermission("rbac.roles.read")]
        public async Task<IActionResult> GetUserPermissions(Guid userId)
        {
            var permissions = await _userRoleService.GetUserPermissionsAsync(userId);
            return Success(permissions);
        }

        /// <summary>
        /// Assign roles to user (Admin only)
        /// TEMP: Authorization disabled for initial admin setup
        /// </summary>
        [HttpPost("users/{userId}/assign")]
        // [RequirePermission("rbac.user_roles.assign")] // TEMP: Commented for initial admin setup
        [AllowAnonymous]
        public async Task<IActionResult> AssignRoles(Guid userId, [FromBody] List<Guid> roleIds)
        {
            await _userRoleService.AssignRolesToUserAsync(userId, roleIds);
            return Success<object?>(null, "Roles assigned successfully");
        }

        /// <summary>
        /// Remove role from user (Admin only)
        /// </summary>
        [HttpDelete("users/{userId}/roles/{roleId}")]
        [RequirePermission("rbac.user_roles.remove")]
        public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId)
        {
            await _userRoleService.RemoveRoleFromUserAsync(userId, roleId);
            return NoContentResponse();
        }

        /// <summary>
        /// Replace user roles (Admin only)
        /// </summary>
        [HttpPut("users/{userId}/replace")]
        [RequirePermission("rbac.user_roles.assign")]
        public async Task<IActionResult> ReplaceRoles(Guid userId, [FromBody] List<Guid> roleIds)
        {
            await _userRoleService.ReplaceUserRolesAsync(userId, roleIds);
            return Success<object?>(null, "User roles replaced successfully");
        }
    }
}
