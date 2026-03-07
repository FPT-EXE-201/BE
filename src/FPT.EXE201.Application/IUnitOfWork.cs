using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.IRepositories;

namespace FPT.EXE201.Application
{
    /// <summary>
    /// Unit of Work pattern for transaction management
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repositories
        IUserRepository Users { get; }
        IUserProfileRepository UserProfiles { get; }
        ILanguageRepository Languages { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IRoleRepository Roles { get; }
        IPermissionRepository Permissions { get; }
        IUserRoleRepository UserRoles { get; }

        // Week 3 — Pregnancy Core
        IPregnancyRepository Pregnancies { get; }
        IPregnancyConditionRepository PregnancyConditions { get; }
        IPrenatalVisitRepository PrenatalVisits { get; }
        IPrenatalTestRepository PrenatalTests { get; }
        IRefPregnancyConditionRepository RefPregnancyConditions { get; }
        IRefTestTypeRepository RefTestTypes { get; }

        // Week 4 — File Storage + Medical Documents
        IStorageFileRepository StorageFiles { get; }
        IMedicalDocumentRepository MedicalDocuments { get; }
        IDocumentFileRepository DocumentFiles { get; }
        IOcrResultRepository OcrResults { get; }
        IRefDocumentTypeRepository RefDocumentTypes { get; }

        // Week 5 — AI Infrastructure
        IAiPromptTemplateRepository AiPromptTemplates { get; }

        // Week 6 — Weight Tracking & Motivational
        IWeightLogRepository WeightLogs { get; }
        IWeightGoalRangeRepository WeightGoalRanges { get; }
        IWeightAlertRepository WeightAlerts { get; }
        IMotivationalTemplateRepository MotivationalTemplates { get; }

        // Week 7 — Nutrition + Meal Planning
        IRefFoodItemRepository RefFoodItems { get; }
        IRefNutrientRepository RefNutrients { get; }
        IPregnancyFoodPreferenceRepository FoodPreferences { get; }
        IPregnancyNutritionNoteRepository NutritionNotes { get; }
        IRecipeRepository Recipes { get; }
        IMealPlanRepository MealPlans { get; }
        IMealPlanDayRepository MealPlanDays { get; }
        IMealItemRepository MealItems { get; }
        IMealPlanFeedbackRepository MealPlanFeedbacks { get; }
        IMealItemFeedbackRepository MealItemFeedbacks { get; }
        IAiRequestLogRepository AiRequestLogs { get; }

        // Transaction methods
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
