using AutoMapper;
using FPT.EXE201.Application.DTOs.Auth;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.MapperConfigs
{
    /// <summary>
    /// AutoMapper profile for User and Auth-related mappings
    /// </summary>
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // RegisterRequestDto -> User
            CreateMap<RegisterRequestDto, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.EmailNormalized, opt => opt.MapFrom(src => src.Email.Trim().ToLowerInvariant()))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Set manually after hashing
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.UserStatus.Active))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsPhoneVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.Profile, opt => opt.Ignore());

            // RegisterRequestDto -> UserProfile
            CreateMap<RegisterRequestDto, UserProfile>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.PreferredLang, opt => opt.MapFrom(src => src.PreferredLanguage ?? "vi"))
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.Ignore())
                .ForMember(dest => dest.AvatarUrl, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.PreferredLanguage, opt => opt.Ignore());

            // User -> UserResponseDto (with Profile)
            CreateMap<User, UserResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.IsEmailVerified))
                .ForMember(dest => dest.IsPhoneVerified, opt => opt.MapFrom(src => src.IsPhoneVerified))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.FullName : null))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.DateOfBirth : null))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.AvatarUrl : null))
                .ForMember(dest => dest.PreferredLanguage, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.PreferredLang : "vi"));
        }
    }
}
