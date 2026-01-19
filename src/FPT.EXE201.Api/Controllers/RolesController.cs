using FPT.EXE201.Api.Controllers;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.RBAC;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers
{
    [Route("api/roles")]
    [Authorize]
    public class RolesController : BaseApiController
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        [HttpGet]
        [RequirePermission("rbac.roles.read")]
        public async Task<IActionResult> GetAll([FromQuery] bool includePermissions = false)
        {
            var roles = await _roleService.GetAllAsync(includePermissions);
            return Success(roles);
        }

        /// <summary>
        /// Get paged roles
        /// </summary>
        [HttpGet("paged")]
        [RequirePermission("rbac.roles.read")]
        public async Task<IActionResult> GetPaged([FromQuery] QueryOptions options)
        {
            var pagedRoles = await _roleService.GetPagedAsync(options);
            return Success(pagedRoles);
        }

        /// <summary>
        /// Get role by ID
        /// </summary>
        [HttpGet("{id}")]
        [RequirePermission("rbac.roles.read")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var role = await _roleService.GetByIdAsync(id);
            return Success(role);
        }

        /// <summary>
        /// Get role by code
        /// </summary>
        [HttpGet("code/{code}")]
        [RequirePermission("rbac.roles.read")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var role = await _roleService.GetByCodeAsync(code);
            return Success(role);
        }

        /// <summary>
        /// Create new role (Admin only)
        /// </summary>
        [HttpPost]
        [RequirePermission("rbac.roles.write")]
        public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
        {
            var role = await _roleService.CreateAsync(dto);
            return Created(role, "Role created successfully");
        }

        /// <summary>
        /// Update role (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [RequirePermission("rbac.roles.write")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleDto dto)
        {
            var role = await _roleService.UpdateAsync(id, dto);
            return Success(role, "Role updated successfully");
        }

        /// <summary>
        /// Delete role (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [RequirePermission("rbac.roles.write")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _roleService.DeleteAsync(id);
            return NoContentResponse();
        }

        /// <summary>
        /// Get role permissions
        /// </summary>
        [HttpGet("{id}/permissions")]
        [RequirePermission("rbac.roles.read")]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            var permissions = await _roleService.GetRolePermissionsAsync(id);
            return Success(permissions);
        }

        /// <summary>
        /// Update role permissions (Admin only)
        /// </summary>
        [HttpPut("{id}/permissions")]
        [RequirePermission("rbac.roles.write")]
        public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] List<Guid> permissionIds)
        {
            await _roleService.UpdateRolePermissionsAsync(id, permissionIds);
            return Success<object?>(null, "Role permissions updated successfully");
        }
    }
}
