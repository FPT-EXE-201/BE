using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class PregnancyConditionSeeder
{
    private static readonly DateTime SeedDate =
        new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Guid GestDiabetes   = Guid.Parse("a0000001-0000-0000-0000-000000000001");
    private static readonly Guid Preeclampsia   = Guid.Parse("a0000001-0000-0000-0000-000000000002");
    private static readonly Guid Anemia         = Guid.Parse("a0000001-0000-0000-0000-000000000003");
    private static readonly Guid Hyperemesis    = Guid.Parse("a0000001-0000-0000-0000-000000000004");
    private static readonly Guid PlacentaPrevia = Guid.Parse("a0000001-0000-0000-0000-000000000005");
    private static readonly Guid Hypertension   = Guid.Parse("a0000001-0000-0000-0000-000000000006");
    private static readonly Guid ThyroidDis     = Guid.Parse("a0000001-0000-0000-0000-000000000007");
    private static readonly Guid GroupBStrep    = Guid.Parse("a0000001-0000-0000-0000-000000000008");
    private static readonly Guid CervicalInsuf  = Guid.Parse("a0000001-0000-0000-0000-000000000009");
    private static readonly Guid EctopicPreg    = Guid.Parse("a0000001-0000-0000-0000-00000000000a");

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefPregnancyCondition>().HasData(
            new { Id = GestDiabetes,   Code = "GESTATIONAL_DIABETES",   IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Preeclampsia,   Code = "PREECLAMPSIA",           IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Anemia,         Code = "ANEMIA",                 IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Hyperemesis,    Code = "HYPEREMESIS_GRAVIDARUM", IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = PlacentaPrevia, Code = "PLACENTA_PREVIA",        IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Hypertension,   Code = "HYPERTENSION",           IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = ThyroidDis,     Code = "THYROID_DISORDER",       IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = GroupBStrep,    Code = "GROUP_B_STREP",          IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = CervicalInsuf,  Code = "CERVICAL_INSUFFICIENCY", IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = EctopicPreg,    Code = "ECTOPIC_PREGNANCY",      IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate }
        );

        modelBuilder.Entity<RefPregnancyConditionTranslation>().HasData(
            // Vietnamese
            new { ConditionId = GestDiabetes,   LanguageCode = "vi", DisplayName = "Tiểu đường thai kỳ",         Description = (string?)"Tình trạng đường huyết cao phát triển trong thai kỳ" },
            new { ConditionId = Preeclampsia,   LanguageCode = "vi", DisplayName = "Tiền sản giật",              Description = (string?)"Huyết áp cao và protein niệu sau tuần 20" },
            new { ConditionId = Anemia,         LanguageCode = "vi", DisplayName = "Thiếu máu",                  Description = (string?)"Lượng hồng cầu hoặc hemoglobin thấp" },
            new { ConditionId = Hyperemesis,    LanguageCode = "vi", DisplayName = "Nghén nặng",                 Description = (string?)"Buồn nôn và nôn nghiêm trọng trong thai kỳ" },
            new { ConditionId = PlacentaPrevia, LanguageCode = "vi", DisplayName = "Nhau tiền đạo",              Description = (string?)"Nhau thai che phủ cổ tử cung" },
            new { ConditionId = Hypertension,   LanguageCode = "vi", DisplayName = "Tăng huyết áp thai kỳ",      Description = (string?)"Huyết áp cao phát hiện sau tuần 20" },
            new { ConditionId = ThyroidDis,     LanguageCode = "vi", DisplayName = "Rối loạn tuyến giáp",        Description = (string?)"Cường giáp hoặc suy giáp trong thai kỳ" },
            new { ConditionId = GroupBStrep,    LanguageCode = "vi", DisplayName = "Nhiễm liên cầu nhóm B",      Description = (string?)"Vi khuẩn GBS có thể lây sang con khi sinh" },
            new { ConditionId = CervicalInsuf,  LanguageCode = "vi", DisplayName = "Hở eo cổ tử cung",           Description = (string?)"Cổ tử cung mở sớm, nguy cơ sinh non" },
            new { ConditionId = EctopicPreg,    LanguageCode = "vi", DisplayName = "Thai ngoài tử cung",         Description = (string?)"Thai làm tổ ngoài buồng tử cung" },
            // English
            new { ConditionId = GestDiabetes,   LanguageCode = "en", DisplayName = "Gestational Diabetes",       Description = (string?)"High blood sugar that develops during pregnancy" },
            new { ConditionId = Preeclampsia,   LanguageCode = "en", DisplayName = "Preeclampsia",               Description = (string?)"High blood pressure and protein in urine after 20 weeks" },
            new { ConditionId = Anemia,         LanguageCode = "en", DisplayName = "Anemia",                     Description = (string?)"Low red blood cell count or hemoglobin" },
            new { ConditionId = Hyperemesis,    LanguageCode = "en", DisplayName = "Hyperemesis Gravidarum",     Description = (string?)"Severe nausea and vomiting during pregnancy" },
            new { ConditionId = PlacentaPrevia, LanguageCode = "en", DisplayName = "Placenta Previa",            Description = (string?)"Placenta covers the cervix" },
            new { ConditionId = Hypertension,   LanguageCode = "en", DisplayName = "Gestational Hypertension",   Description = (string?)"High blood pressure after week 20 without proteinuria" },
            new { ConditionId = ThyroidDis,     LanguageCode = "en", DisplayName = "Thyroid Disorder",           Description = (string?)"Hyperthyroidism or hypothyroidism during pregnancy" },
            new { ConditionId = GroupBStrep,    LanguageCode = "en", DisplayName = "Group B Streptococcus",      Description = (string?)"GBS bacteria that may pass to baby during delivery" },
            new { ConditionId = CervicalInsuf,  LanguageCode = "en", DisplayName = "Cervical Insufficiency",     Description = (string?)"Cervix opens prematurely, risk of preterm birth" },
            new { ConditionId = EctopicPreg,    LanguageCode = "en", DisplayName = "Ectopic Pregnancy",          Description = (string?)"Pregnancy implanted outside the uterus" }
        );
    }
}
