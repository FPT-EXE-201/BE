using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class TestTypeSeeder
{
    private static readonly DateTime SeedDate =
        new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Guid Biochemistry = Guid.Parse("b0000002-0000-0000-0000-000000000001");
    private static readonly Guid Ultrasound   = Guid.Parse("b0000002-0000-0000-0000-000000000002");
    private static readonly Guid BloodPress   = Guid.Parse("b0000002-0000-0000-0000-000000000003");
    private static readonly Guid CBC          = Guid.Parse("b0000002-0000-0000-0000-000000000004");
    private static readonly Guid UrineTest    = Guid.Parse("b0000002-0000-0000-0000-000000000005");
    private static readonly Guid HepB         = Guid.Parse("b0000002-0000-0000-0000-000000000006");
    private static readonly Guid HIV          = Guid.Parse("b0000002-0000-0000-0000-000000000007");
    private static readonly Guid TSH          = Guid.Parse("b0000002-0000-0000-0000-000000000008");
    private static readonly Guid NTScan       = Guid.Parse("b0000002-0000-0000-0000-000000000009");
    private static readonly Guid OGTT         = Guid.Parse("b0000002-0000-0000-0000-00000000000a");
    private static readonly Guid BloodTest    = Guid.Parse("b0000002-0000-0000-0000-00000000000b");
    private static readonly Guid CBCTest      = Guid.Parse("b0000002-0000-0000-0000-00000000000c");

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefTestType>().HasData(
            new { Id = Biochemistry, Code = "BIOCHEMISTRY",          Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Ultrasound,   Code = "ULTRASOUND",            Category = (string?)"IMAGING", IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = BloodPress,   Code = "BLOOD_PRESSURE",        Category = (string?)"OTHER",   IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = CBC,          Code = "COMPLETE_BLOOD_COUNT",   Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = UrineTest,    Code = "URINE_TEST",            Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = HepB,         Code = "HEPATITIS_B",           Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = HIV,          Code = "HIV_SCREEN",            Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = TSH,          Code = "TSH",                   Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = NTScan,       Code = "NT_SCAN",               Category = (string?)"IMAGING", IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = OGTT,         Code = "OGTT",                  Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = BloodTest,    Code = "BLOOD_TEST",            Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = CBCTest,      Code = "CBC_TEST",              Category = (string?)"LAB",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate }
        );

        modelBuilder.Entity<RefTestTypeTranslation>().HasData(
            // Vietnamese
            new { TestTypeId = Biochemistry, LanguageCode = "vi", DisplayName = "Xét nghiệm hoá sinh máu",        Description = (string?)"Kiểm tra các chỉ số hoá sinh trong máu (đường, mỡ, gan, thận, điện giải)" },
            new { TestTypeId = Ultrasound,   LanguageCode = "vi", DisplayName = "Siêu âm",                         Description = (string?)"Chụp hình ảnh thai nhi bằng sóng siêu âm" },
            new { TestTypeId = BloodPress,   LanguageCode = "vi", DisplayName = "Đo huyết áp",                     Description = (string?)"Đo áp lực máu trong động mạch" },
            new { TestTypeId = CBC,          LanguageCode = "vi", DisplayName = "Công thức máu toàn phần",         Description = (string?)"Đếm số lượng và phân loại tế bào máu" },
            new { TestTypeId = UrineTest,    LanguageCode = "vi", DisplayName = "Xét nghiệm nước tiểu",           Description = (string?)"Phân tích thành phần nước tiểu" },
            new { TestTypeId = HepB,         LanguageCode = "vi", DisplayName = "Xét nghiệm viêm gan B",          Description = (string?)"Tầm soát virus viêm gan B" },
            new { TestTypeId = HIV,          LanguageCode = "vi", DisplayName = "Xét nghiệm HIV",                 Description = (string?)"Tầm soát virus HIV" },
            new { TestTypeId = TSH,          LanguageCode = "vi", DisplayName = "Xét nghiệm TSH",                 Description = (string?)"Kiểm tra chức năng tuyến giáp" },
            new { TestTypeId = NTScan,       LanguageCode = "vi", DisplayName = "Đo độ mờ da gáy",                Description = (string?)"Siêu âm tầm soát dị tật thai nhi" },
            new { TestTypeId = OGTT,         LanguageCode = "vi", DisplayName = "Nghiệm pháp dung nạp glucose",   Description = (string?)"Xét nghiệm chẩn đoán tiểu đường thai kỳ" },
            new { TestTypeId = BloodTest,    LanguageCode = "vi", DisplayName = "Xét nghiệm máu",                  Description = (string?)"Xét nghiệm máu tổng quát" },
            new { TestTypeId = CBCTest,      LanguageCode = "vi", DisplayName = "Xét nghiệm công thức máu",       Description = (string?)"Phân tích thành phần tế bào máu" },
            // English
            new { TestTypeId = Biochemistry, LanguageCode = "en", DisplayName = "Blood Biochemistry Panel",        Description = (string?)"Comprehensive blood chemistry (glucose, lipids, liver, kidney, electrolytes)" },
            new { TestTypeId = Ultrasound,   LanguageCode = "en", DisplayName = "Ultrasound",                      Description = (string?)"Imaging of fetus using sound waves" },
            new { TestTypeId = BloodPress,   LanguageCode = "en", DisplayName = "Blood Pressure",                  Description = (string?)"Measures blood pressure in arteries" },
            new { TestTypeId = CBC,          LanguageCode = "en", DisplayName = "Complete Blood Count",            Description = (string?)"Counts different blood cell types" },
            new { TestTypeId = UrineTest,    LanguageCode = "en", DisplayName = "Urine Test",                      Description = (string?)"Analyzes urine composition" },
            new { TestTypeId = HepB,         LanguageCode = "en", DisplayName = "Hepatitis B Screen",              Description = (string?)"Screens for hepatitis B virus" },
            new { TestTypeId = HIV,          LanguageCode = "en", DisplayName = "HIV Screen",                      Description = (string?)"Screens for HIV virus" },
            new { TestTypeId = TSH,          LanguageCode = "en", DisplayName = "TSH Test",                        Description = (string?)"Checks thyroid function" },
            new { TestTypeId = NTScan,       LanguageCode = "en", DisplayName = "Nuchal Translucency Scan",        Description = (string?)"Ultrasound screening for fetal abnormalities" },
            new { TestTypeId = OGTT,         LanguageCode = "en", DisplayName = "Oral Glucose Tolerance Test",     Description = (string?)"Diagnostic test for gestational diabetes" },
            new { TestTypeId = BloodTest,    LanguageCode = "en", DisplayName = "Blood Test",                      Description = (string?)"General blood test" },
            new { TestTypeId = CBCTest,      LanguageCode = "en", DisplayName = "CBC Test",                        Description = (string?)"Complete blood count test" }
        );
    }
}
