using AutoMapper;
using FPT.EXE201.Application.DTOs.RBAC;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services
{
    public class UserRoleService : IUserRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserRoleService(IUnitOfWork unitOfWork, IMapper _mapper)
        {
            _unitOfWork = unitOfWork;
            this._mapper = _mapper;
        }

        public async Task<List<UserRoleDto>> GetUserRolesAsync(Guid userId, CancellationToken ct = default)
        {
            // Verify user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId, includeDeleted: false, cancellationToken: ct);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} not found");
            }

            var userRoles = await _unitOfWork.UserRoles.GetByUserIdAsync(userId, ct);
            
            return userRoles.Select(ur => new UserRoleDto
            {
                UserId = ur.UserId,
                RoleId = ur.RoleId,
                RoleCode = ur.Role.Code,
                RoleName = ur.Role.Name,
                AssignedAt = ur.CreatedAt
            }).ToList();
        }

        public async Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default)
        {
            return await _unitOfWork.UserRoles.GetUserPermissionCodesAsync(userId, ct);
        }

        public async Task<List<string>> GetUserPermissionCodesAsync(Guid userId, CancellationToken ct = default)
        {
            return await _unitOfWork.UserRoles.GetUserPermissionCodesAsync(userId, ct);
        }

        public async Task<List<string>> GetUserRoleCodesAsync(Guid userId, CancellationToken ct = default)
        {
            return await _unitOfWork.UserRoles.GetUserRoleCodesAsync(userId, ct);
        }

        public async Task AssignRolesToUserAsync(Guid userId, List<Guid> roleIds, CancellationToken ct = default)
        {
            // Verify user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId, includeDeleted: false, cancellationToken: ct);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} not found");
            }

            // Verify all roles exist
            foreach (var roleId in roleIds)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(roleId, includeDeleted: false, cancellationToken: ct);
                if (role == null)
                {
                    throw new NotFoundException($"Role with ID {roleId} not found");
                }

                // Check if already assigned
                if (await _unitOfWork.UserRoles.ExistsAsync(userId, roleId, ct))
                {
                    continue; // Skip if already assigned
                }

                // Assign role
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId
                };

                await _unitOfWork.UserRoles.AddAsync(userRole, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        {
            var userRole = await _unitOfWork.UserRoles.GetByUserAndRoleAsync(userId, roleId, ct);
            
            if (userRole == null)
            {
                throw new NotFoundException($"User {userId} does not have role {roleId}");
            }

            await _unitOfWork.UserRoles.RemoveAsync(userRole, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task ReplaceUserRolesAsync(Guid userId, List<Guid> roleIds, CancellationToken ct = default)
        {
            // Verify user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId, includeDeleted: false, cancellationToken: ct);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} not found");
            }

            // Verify all roles exist
            foreach (var roleId in roleIds)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(roleId, includeDeleted: false, cancellationToken: ct);
                if (role == null)
                {
                    throw new NotFoundException($"Role with ID {roleId} not found");
                }
            }

            // Remove all existing roles
            var existingRoles = await _unitOfWork.UserRoles.GetByUserIdAsync(userId, ct);
            if (existingRoles.Any())
            {
                await _unitOfWork.UserRoles.RemoveRangeAsync(existingRoles, ct);
            }

            // Add new roles
            var newUserRoles = roleIds.Select(roleId => new UserRole
            {
                UserId = userId,
                RoleId = roleId
            }).ToList();

            foreach (var userRole in newUserRoles)
            {
                await _unitOfWork.UserRoles.AddAsync(userRole, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken ct = default)
        {
            var permissions = await _unitOfWork.UserRoles.GetUserPermissionCodesAsync(userId, ct);
            return permissions.Contains(permissionCode);
        }

        public async Task<bool> HasRoleAsync(Guid userId, string roleCode, CancellationToken ct = default)
        {
            var roles = await _unitOfWork.UserRoles.GetUserRoleCodesAsync(userId, ct);
            return roles.Contains(roleCode);
        }
    }
}
