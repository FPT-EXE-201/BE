using AutoMapper;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.RBAC;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RoleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var role = await _unitOfWork.Roles.GetByIdWithPermissionsAsync(id, includeDeleted: false, ct);
            return role == null ? null : _mapper.Map<RoleDto>(role);
        }

        public async Task<RoleDto?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            var role = await _unitOfWork.Roles.GetByCodeAsync(code, includeDeleted: false, ct);
            return role == null ? null : _mapper.Map<RoleDto>(role);
        }

        public async Task<List<RoleDto>> GetAllAsync(bool includePermissions = false, CancellationToken ct = default)
        {
            List<Role> roles;
            
            if (includePermissions)
            {
                roles = await _unitOfWork.Roles.GetAllWithPermissionsAsync(includeDeleted: false, ct);
            }
            else
            {
                var allRoles = await _unitOfWork.Roles.GetAllAsync(includeDeleted: false, cancellationToken: ct);
                roles = allRoles.ToList();
            }

            return _mapper.Map<List<RoleDto>>(roles);
        }

        public async Task<PagedResult<RoleDto>> GetPagedAsync(QueryOptions options, CancellationToken ct = default)
        {
            var pagedRoles = await _unitOfWork.Roles.GetPagedRolesAsync(options, ct);
            
            return new PagedResult<RoleDto>
            {
                Items = _mapper.Map<List<RoleDto>>(pagedRoles.Items),
                TotalItems = pagedRoles.TotalItems,
                Page = pagedRoles.Page,
                PageSize = pagedRoles.PageSize
            };
        }

        public async Task<RoleDto> CreateAsync(CreateRoleDto dto, CancellationToken ct = default)
        {
            // Validate code uniqueness
            if (await _unitOfWork.Roles.ExistsByCodeAsync(dto.Code, excludeId: null, includeDeleted: false, ct))
            {
                throw new ConflictException($"Role with code '{dto.Code}' already exists");
            }

            var role = _mapper.Map<Role>(dto);
            await _unitOfWork.Roles.AddAsync(role, ct);

            // Add role permissions if provided
            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                var permissions = await _unitOfWork.Permissions.GetByIdsAsync(dto.PermissionIds, includeDeleted: false, ct);
                
                if (permissions.Count != dto.PermissionIds.Count)
                {
                    throw new BadRequestException("One or more permission IDs are invalid");
                }

                var rolePermissions = permissions.Select(p => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id
                }).ToList();

                foreach (var rp in rolePermissions)
                {
                    await _unitOfWork.Roles.AddRolePermissionAsync(rp, ct);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);

            // Reload with permissions
            var createdRole = await _unitOfWork.Roles.GetByIdWithPermissionsAsync(role.Id, includeDeleted: false, ct);
            return _mapper.Map<RoleDto>(createdRole);
        }

        public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto dto, CancellationToken ct = default)
        {
            var role = await _unitOfWork.Roles.GetByIdTrackedAsync(id, includeDeleted: false, cancellationToken: ct);
            
            if (role == null)
            {
                throw new NotFoundException($"Role with ID {id} not found");
            }

            // Prevent updating system roles (ADMIN, USER, DOCTOR)
            if (role.Code == "ADMIN" || role.Code == "USER" || role.Code == "DOCTOR")
            {
                throw new ForbiddenException($"Cannot modify system role '{role.Code}'");
            }

            // Update basic properties
            role.Name = dto.Name;
            role.Description = dto.Description;

            // UpdateAsync is from IGenericRepository base, no need to call it explicitly
            // EF Core tracks changes automatically

            // Update permissions if provided
            if (dto.PermissionIds != null)
            {
                await UpdateRolePermissionsAsync(id, dto.PermissionIds, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            // Reload with permissions
            var updatedRole = await _unitOfWork.Roles.GetByIdWithPermissionsAsync(id, includeDeleted: false, ct);
            return _mapper.Map<RoleDto>(updatedRole);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var role = await _unitOfWork.Roles.GetByIdTrackedAsync(id, includeDeleted: false, cancellationToken: ct);
            
            if (role == null)
            {
                throw new NotFoundException($"Role with ID {id} not found");
            }

            // Prevent deleting system roles
            if (role.Code == "ADMIN" || role.Code == "USER" || role.Code == "DOCTOR")
            {
                throw new ForbiddenException($"Cannot delete system role '{role.Code}'");
            }

            // Soft delete
            await _unitOfWork.Roles.SoftDeleteAsync(role, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default)
        {
            var permissions = await _unitOfWork.Permissions.GetByRoleIdAsync(roleId, includeDeleted: false, ct);
            return _mapper.Map<List<PermissionDto>>(permissions);
        }

        public async Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds, CancellationToken ct = default)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId, includeDeleted: false, cancellationToken: ct);
            
            if (role == null)
            {
                throw new NotFoundException($"Role with ID {roleId} not found");
            }

            // Validate all permissions exist
            var permissions = await _unitOfWork.Permissions.GetByIdsAsync(permissionIds, includeDeleted: false, ct);
            
            if (permissions.Count != permissionIds.Count)
            {
                throw new BadRequestException("One or more permission IDs are invalid");
            }

            // Remove existing role permissions
            await _unitOfWork.Roles.RemoveRolePermissionsAsync(roleId, ct);

            // Add new role permissions
            foreach (var permId in permissionIds)
            {
                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permId
                };
                await _unitOfWork.Roles.AddRolePermissionAsync(rolePermission, ct);
            }
        }
    }
}
