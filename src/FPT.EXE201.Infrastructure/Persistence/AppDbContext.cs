using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<AuthRefreshToken> AuthRefreshTokens { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }

        // Week 3 — Pregnancy Core
        public DbSet<Pregnancy> Pregnancies { get; set; }
        public DbSet<RefPregnancyCondition> RefPregnancyConditions { get; set; }
        public DbSet<RefPregnancyConditionTranslation> RefPregnancyConditionTranslations { get; set; }
        public DbSet<PregnancyCondition> PregnancyConditions { get; set; }
        public DbSet<PrenatalVisit> PrenatalVisits { get; set; }
        public DbSet<RefTestType> RefTestTypes { get; set; }
        public DbSet<RefTestTypeTranslation> RefTestTypeTranslations { get; set; }
        public DbSet<PrenatalTest> PrenatalTests { get; set; }

        // Week 4 — Medical Documents & Storage
        public DbSet<StorageFile> StorageFiles { get; set; }
        public DbSet<RefDocumentType> RefDocumentTypes { get; set; }
        public DbSet<RefDocumentTypeTranslation> RefDocumentTypeTranslations { get; set; }
        public DbSet<MedicalDocument> MedicalDocuments { get; set; }
        public DbSet<DocumentFile> DocumentFiles { get; set; }
        public DbSet<OcrResult> OcrResults { get; set; }

        // Week 5 — AI Prompt Templates
        public DbSet<AiPromptTemplate> AiPromptTemplates { get; set; }

        // Week 6 — Weight Tracking & Motivational
        public DbSet<WeightLog> WeightLogs { get; set; }
        public DbSet<WeightGoalRange> WeightGoalRanges { get; set; }
        public DbSet<WeightAlert> WeightAlerts { get; set; }
        public DbSet<MotivationalTemplate> MotivationalTemplates { get; set; }
        public DbSet<MotivationalTemplateTranslation> MotivationalTemplateTranslations { get; set; }

        // Week 7 — Nutrition + Meal Planning
        public DbSet<RefFoodItem> RefFoodItems { get; set; }
        public DbSet<RefFoodItemTranslation> RefFoodItemTranslations { get; set; }
        public DbSet<RefNutrient> RefNutrients { get; set; }
        public DbSet<RefNutrientTranslation> RefNutrientTranslations { get; set; }
        public DbSet<PregnancyFoodPreference> PregnancyFoodPreferences { get; set; }
        public DbSet<PregnancyNutritionNote> PregnancyNutritionNotes { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<MealPlanDay> MealPlanDays { get; set; }
        public DbSet<MealItem> MealItems { get; set; }
        public DbSet<MealItemNutrient> MealItemNutrients { get; set; }
        public DbSet<MealPlanFeedback> MealPlanFeedbacks { get; set; }
        public DbSet<MealItemFeedback> MealItemFeedbacks { get; set; }
        public DbSet<AiRequestLog> AiRequestLogs { get; set; }

        // Chat
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasCharSet("utf8mb4");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            PregnancyConditionSeeder.Seed(modelBuilder);
            TestTypeSeeder.Seed(modelBuilder);
            DocumentTypeSeeder.Seed(modelBuilder);

            MotivationalTemplateSeeder.Seed(modelBuilder);
            NutritionFoodItemSeeder.Seed(modelBuilder);
            NutrientSeeder.Seed(modelBuilder);

            // Apply soft delete query filter for all entities inheriting from BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
                    var nullConstant = System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?));
                    var equality = System.Linq.Expressions.Expression.Equal(property, nullConstant);
                    var lambda = System.Linq.Expressions.Expression.Lambda(equality, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        // Override SaveChanges to automatically update timestamps
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                // Handle BaseEntity
                if (entry.Entity is BaseEntity baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        baseEntity.CreatedAt = DateTime.UtcNow;
                    }
                    baseEntity.UpdatedAt = DateTime.UtcNow;
                }
                // Handle AuthRefreshToken
                else if (entry.Entity is AuthRefreshToken refreshToken)
                {
                    if (entry.State == EntityState.Added)
                    {
                        refreshToken.CreatedAt = DateTime.UtcNow;
                    }
                    refreshToken.UpdatedAt = DateTime.UtcNow;
                }
                // Handle AuditEvent
                else if (entry.Entity is AuditEvent auditEvent && entry.State == EntityState.Added)
                {
                    auditEvent.CreatedAt = DateTime.UtcNow;
                }
                // Handle RolePermission 
                else if (entry.Entity is RolePermission rolePermission && entry.State == EntityState.Added)
                {
                    rolePermission.CreatedAt = DateTime.UtcNow;
                }
                // Handle UserRole 
                else if (entry.Entity is UserRole userRole && entry.State == EntityState.Added)
                {
                    userRole.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is RefNutrient nutrient)
                {
                    if (entry.State == EntityState.Added)
                    {
                        nutrient.CreatedAt = DateTime.UtcNow;
                    }
                    nutrient.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
