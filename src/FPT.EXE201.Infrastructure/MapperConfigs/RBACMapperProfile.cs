using AutoMapper;
using FPT.EXE201.Application.DTOs.RBAC;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.MapperConfigs
{
    /// <summary>
    /// AutoMapper profile for RBAC entities (auto-scanned by AddAutoMapper)
    /// </summary>
    public class RBACMapperProfile : Profile
    {
        public RBACMapperProfile()
        {
            // Role mappings
            CreateMap<Role, RoleDto>()
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src =>
                    src.RolePermissions.Select(rp => rp.Permission).ToList()));

            CreateMap<CreateRoleDto, Role>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.RolePermissions, opt => opt.Ignore())
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

            // Permission mappings
            CreateMap<Permission, PermissionDto>();

            // UserRole mappings
            CreateMap<UserRole, UserRoleDto>()
                .ForMember(dest => dest.RoleCode, opt => opt.MapFrom(src => src.Role.Code))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
                .ForMember(dest => dest.AssignedAt, opt => opt.MapFrom(src => src.CreatedAt));
        }
    }
}
