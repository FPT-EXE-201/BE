using FluentValidation;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FPT.EXE201.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register AutoMapper (scan Application assembly for Profile classes)
        services.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);

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

        // Week 3 - Pregnancy Core Services
        services.AddScoped<IPregnancyService, PregnancyService>();
        services.AddScoped<IPregnancyConditionService, PregnancyConditionService>();
        services.AddScoped<IPrenatalVisitService, PrenatalVisitService>();
        services.AddScoped<IPrenatalTestService, PrenatalTestService>();
        services.AddScoped<IRefDataService, RefDataService>();

        // Week 4 - Medical Documents
        services.AddScoped<IMedicalDocumentService, MedicalDocumentService>();

        // Week 5 — AI Application Services
        services.AddScoped<IMedicalRecordAiService, MedicalRecordAiService>();

        // Week 5.5 — Auto-Fill
        services.AddScoped<IAutoFillService, AutoFillService>();

        // Week 6 — Weight Tracking & Motivational
        services.AddScoped<IWeightLogService, WeightLogService>();
        services.AddScoped<IMotivationalService, MotivationalService>();

        // Week 7 — Nutrition + Meal Planning
        services.AddScoped<IFoodPreferenceService, FoodPreferenceService>();
        services.AddScoped<IMealPlanService, MealPlanService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<INutritionFeedbackService, NutritionFeedbackService>();
        #endregion

        return services;
    }
}
