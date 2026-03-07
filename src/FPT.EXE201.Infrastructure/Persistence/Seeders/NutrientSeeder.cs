using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class NutrientSeeder
{
    private static readonly DateTime SeedDate = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder builder)
    {
        var nutrients = new (string id, string code, string unit, string vi, string en)[]
        {
            ("c7020001-0000-0000-0000-000000000001", "CALORIES",       "kcal", "Năng lượng",   "Calories"),
            ("c7020001-0000-0000-0000-000000000002", "PROTEIN",        "g",    "Chất đạm",     "Protein"),
            ("c7020001-0000-0000-0000-000000000003", "CARBOHYDRATES",  "g",    "Tinh bột",     "Carbohydrates"),
            ("c7020001-0000-0000-0000-000000000004", "FAT",            "g",    "Chất béo",     "Fat"),
            ("c7020001-0000-0000-0000-000000000005", "FIBER",          "g",    "Chất xơ",      "Fiber"),
            ("c7020001-0000-0000-0000-000000000006", "IRON",           "mg",   "Sắt",          "Iron"),
            ("c7020001-0000-0000-0000-000000000007", "CALCIUM",        "mg",   "Canxi",        "Calcium"),
            ("c7020001-0000-0000-0000-000000000008", "FOLIC_ACID",     "mcg",  "Axit folic",   "Folic acid"),
            ("c7020001-0000-0000-0000-000000000009", "VITAMIN_D",      "mcg",  "Vitamin D",    "Vitamin D"),
            ("c7020001-0000-0000-0000-00000000000a", "VITAMIN_C",      "mg",   "Vitamin C",    "Vitamin C"),
            ("c7020001-0000-0000-0000-00000000000b", "VITAMIN_A",      "mcg",  "Vitamin A",    "Vitamin A"),
            ("c7020001-0000-0000-0000-00000000000c", "VITAMIN_B12",    "mcg",  "Vitamin B12",  "Vitamin B12"),
            ("c7020001-0000-0000-0000-00000000000d", "OMEGA_3",        "mg",   "Omega-3",      "Omega-3"),
            ("c7020001-0000-0000-0000-00000000000e", "DHA",            "mg",   "DHA",          "DHA"),
            ("c7020001-0000-0000-0000-00000000000f", "ZINC",           "mg",   "Kẽm",          "Zinc"),
        };

        foreach (var (id, code, unit, vi, en) in nutrients)
        {
            var guid = new Guid(id);

            // RefNutrient (custom entity — NOT BaseEntity, no DeletedAt)
            builder.Entity<RefNutrient>().HasData(new
            {
                Id = guid,
                Code = code,
                Unit = unit,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });

            // Vietnamese translation
            builder.Entity<RefNutrientTranslation>().HasData(new
            {
                NutrientId = guid,
                LanguageCode = "vi",
                DisplayName = vi
            });

            // English translation
            builder.Entity<RefNutrientTranslation>().HasData(new
            {
                NutrientId = guid,
                LanguageCode = "en",
                DisplayName = en
            });
        }
    }
}
