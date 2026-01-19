using FluentValidation;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FPT.EXE201.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        #region Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        
        // RBAC Services
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        #endregion

        return services;
    }
}
