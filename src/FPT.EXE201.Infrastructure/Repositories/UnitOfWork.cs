using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace FPT.EXE201.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work implementation for managing repositories and transactions
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy-initialized repositories
        private IUserRepository? _users;
        private IUserProfileRepository? _userProfiles;
        private ILanguageRepository? _languages;
        private IRefreshTokenRepository? _refreshTokens;
        private IRoleRepository? _roles;
        private IPermissionRepository? _permissions;
        private IUserRoleRepository? _userRoles;

        // Week 3 — Pregnancy Core
        private IPregnancyRepository? _pregnancies;
        private IPregnancyConditionRepository? _pregnancyConditions;
        private IPrenatalVisitRepository? _prenatalVisits;
        private IPrenatalTestRepository? _prenatalTests;
        private IRefPregnancyConditionRepository? _refPregnancyConditions;
        private IRefTestTypeRepository? _refTestTypes;

        // Week 4 — File Storage + Medical Documents
        private IStorageFileRepository? _storageFiles;
        private IMedicalDocumentRepository? _medicalDocuments;
        private IDocumentFileRepository? _documentFiles;
        private IOcrResultRepository? _ocrResults;
        private IRefDocumentTypeRepository? _refDocumentTypes;

        // Week 5 — AI Infrastructure
        private IAiPromptTemplateRepository? _aiPromptTemplates;

        // Week 6 — Weight Tracking & Motivational
        private IWeightLogRepository? _weightLogs;
        private IWeightGoalRangeRepository? _weightGoalRanges;
        private IWeightAlertRepository? _weightAlerts;
        private IMotivationalTemplateRepository? _motivationalTemplates;

        // Week 7 — Premium Subscription
        private ISubscriptionRepository? _subscriptions;

        // Week 7 — Nutrition + Meal Planning
        private IRefFoodItemRepository? _refFoodItems;
        private IRefNutrientRepository? _refNutrients;
        private IPregnancyFoodPreferenceRepository? _foodPreferences;
        private IPregnancyNutritionNoteRepository? _nutritionNotes;
        private IRecipeRepository? _recipes;
        private IMealPlanRepository? _mealPlans;
        private IMealPlanDayRepository? _mealPlanDays;
        private IMealItemRepository? _mealItems;
        private IMealPlanFeedbackRepository? _mealPlanFeedbacks;
        private IMealItemFeedbackRepository? _mealItemFeedbacks;
        private IAiRequestLogRepository? _aiRequestLogs;

        // Chat
        private IChatMessageRepository? _chatMessages;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users => _users ??= new UserRepository(_context);
        public IUserProfileRepository UserProfiles => _userProfiles ??= new UserProfileRepository(_context);
        public ILanguageRepository Languages => _languages ??= new LanguageRepository(_context);
        public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
        public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
        public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
        public IUserRoleRepository UserRoles => _userRoles ??= new UserRoleRepository(_context);

        // Week 3 — Pregnancy Core
        public IPregnancyRepository Pregnancies => _pregnancies ??= new PregnancyRepository(_context);
        public IPregnancyConditionRepository PregnancyConditions => _pregnancyConditions ??= new PregnancyConditionRepository(_context);
        public IPrenatalVisitRepository PrenatalVisits => _prenatalVisits ??= new PrenatalVisitRepository(_context);
        public IPrenatalTestRepository PrenatalTests => _prenatalTests ??= new PrenatalTestRepository(_context);
        public IRefPregnancyConditionRepository RefPregnancyConditions => _refPregnancyConditions ??= new RefPregnancyConditionRepository(_context);
        public IRefTestTypeRepository RefTestTypes => _refTestTypes ??= new RefTestTypeRepository(_context);

        // Week 4 — File Storage + Medical Documents
        public IStorageFileRepository StorageFiles => _storageFiles ??= new StorageFileRepository(_context);
        public IMedicalDocumentRepository MedicalDocuments => _medicalDocuments ??= new MedicalDocumentRepository(_context);
        public IDocumentFileRepository DocumentFiles => _documentFiles ??= new DocumentFileRepository(_context);
        public IOcrResultRepository OcrResults => _ocrResults ??= new OcrResultRepository(_context);
        public IRefDocumentTypeRepository RefDocumentTypes
            => _refDocumentTypes ??= new RefDocumentTypeRepository(_context);

        // Week 5 — AI Infrastructure
        public IAiPromptTemplateRepository AiPromptTemplates
            => _aiPromptTemplates ??= new AiPromptTemplateRepository(_context);

        // Week 6 — Weight Tracking & Motivational
        public IWeightLogRepository WeightLogs => _weightLogs ??= new WeightLogRepository(_context);
        public IWeightGoalRangeRepository WeightGoalRanges => _weightGoalRanges ??= new WeightGoalRangeRepository(_context);
        public IWeightAlertRepository WeightAlerts => _weightAlerts ??= new WeightAlertRepository(_context);
        public IMotivationalTemplateRepository MotivationalTemplates => _motivationalTemplates ??= new MotivationalTemplateRepository(_context);

        // Week 7 — Premium Subscription
        public ISubscriptionRepository Subscriptions => _subscriptions ??= new SubscriptionRepository(_context);

        // Week 7 — Nutrition + Meal Planning
        public IRefFoodItemRepository RefFoodItems => _refFoodItems ??= new RefFoodItemRepository(_context);
        public IRefNutrientRepository RefNutrients => _refNutrients ??= new RefNutrientRepository(_context);
        public IPregnancyFoodPreferenceRepository FoodPreferences => _foodPreferences ??= new PregnancyFoodPreferenceRepository(_context);
        public IPregnancyNutritionNoteRepository NutritionNotes => _nutritionNotes ??= new PregnancyNutritionNoteRepository(_context);
        public IRecipeRepository Recipes => _recipes ??= new RecipeRepository(_context);
        public IMealPlanRepository MealPlans => _mealPlans ??= new MealPlanRepository(_context);
        public IMealPlanDayRepository MealPlanDays => _mealPlanDays ??= new MealPlanDayRepository(_context);
        public IMealItemRepository MealItems => _mealItems ??= new MealItemRepository(_context);
        public IMealPlanFeedbackRepository MealPlanFeedbacks => _mealPlanFeedbacks ??= new MealPlanFeedbackRepository(_context);
        public IMealItemFeedbackRepository MealItemFeedbacks => _mealItemFeedbacks ??= new MealItemFeedbackRepository(_context);
        public IAiRequestLogRepository AiRequestLogs => _aiRequestLogs ??= new AiRequestLogRepository(_context);

        // Chat
        public IChatMessageRepository ChatMessages => _chatMessages ??= new ChatMessageRepository(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await SaveChangesAsync(cancellationToken);
                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
