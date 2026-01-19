using FPT.EXE201.Api.Controllers;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers
{
    [Route("api/permissions")]
    [Authorize]
    public class PermissionsController : BaseApiController
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// Get all permissions
        /// </summary>
        [HttpGet]
        [RequirePermission("rbac.permissions.read")]
        public async Task<IActionResult> GetAll()
        {
            var permissions = await _permissionService.GetAllAsync();
            return Success(permissions);
        }

        /// <summary>
        /// Get paged permissions
        /// </summary>
        [HttpGet("paged")]
        [RequirePermission("rbac.permissions.read")]
        public async Task<IActionResult> GetPaged([FromQuery] QueryOptions options)
        {
            var pagedPermissions = await _permissionService.GetPagedAsync(options);
            return Success(pagedPermissions);
        }

        /// <summary>
        /// Get permission by ID
        /// </summary>
        [HttpGet("{id}")]
        [RequirePermission("rbac.permissions.read")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var permission = await _permissionService.GetByIdAsync(id);
            return Success(permission);
        }

        /// <summary>
        /// Get permission by code
        /// </summary>
        [HttpGet("code/{code}")]
        [RequirePermission("rbac.permissions.read")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var permission = await _permissionService.GetByCodeAsync(code);
            return Success(permission);
        }
    }
}
