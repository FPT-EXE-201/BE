using AutoMapper;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.RBAC;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Application.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PermissionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PermissionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var permission = await _unitOfWork.Permissions.GetByIdAsync(id, includeDeleted: false, cancellationToken: ct);
            return permission == null ? null : _mapper.Map<PermissionDto>(permission);
        }

        public async Task<PermissionDto?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            var permission = await _unitOfWork.Permissions.GetByCodeAsync(code, includeDeleted: false, ct);
            return permission == null ? null : _mapper.Map<PermissionDto>(permission);
        }

        public async Task<List<PermissionDto>> GetAllAsync(CancellationToken ct = default)
        {
            var permissions = await _unitOfWork.Permissions.GetAllAsync(includeDeleted: false, cancellationToken: ct);
            return _mapper.Map<List<PermissionDto>>(permissions);
        }

        public async Task<PagedResult<PermissionDto>> GetPagedAsync(QueryOptions options, CancellationToken ct = default)
        {
            var pagedPermissions = await _unitOfWork.Permissions.GetPagedPermissionsAsync(options, ct);
            
            return new PagedResult<PermissionDto>
            {
                Items = _mapper.Map<List<PermissionDto>>(pagedPermissions.Items),
                TotalItems = pagedPermissions.TotalItems,
                Page = pagedPermissions.Page,
                PageSize = pagedPermissions.PageSize
            };
        }
    }
}
