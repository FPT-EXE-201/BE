using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class DocumentTypeSeeder
{
    private static readonly DateTime SeedDate =
        new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Guid PrenatalCheckup   = Guid.Parse("b0000001-0000-0000-0000-000000000001");
    private static readonly Guid Ultrasound        = Guid.Parse("b0000001-0000-0000-0000-000000000002");
    private static readonly Guid BloodTest         = Guid.Parse("b0000001-0000-0000-0000-000000000003");
    private static readonly Guid UrineTest         = Guid.Parse("b0000001-0000-0000-0000-000000000004");
    private static readonly Guid Prescription      = Guid.Parse("b0000001-0000-0000-0000-000000000005");
    private static readonly Guid VaccinationRecord = Guid.Parse("b0000001-0000-0000-0000-000000000006");
    private static readonly Guid MedicalReport     = Guid.Parse("b0000001-0000-0000-0000-000000000007");
    private static readonly Guid Other             = Guid.Parse("b0000001-0000-0000-0000-000000000008");
    // Week 5.5: Specific test document types for direct matching
    private static readonly Guid HivTest           = Guid.Parse("b0000001-0000-0000-0000-000000000009");
    private static readonly Guid HepatitisBTest    = Guid.Parse("b0000001-0000-0000-0000-00000000000a");
    private static readonly Guid ThyroidTest       = Guid.Parse("b0000001-0000-0000-0000-00000000000b");
    private static readonly Guid GlucoseTest       = Guid.Parse("b0000001-0000-0000-0000-00000000000c");
    private static readonly Guid CbcTest           = Guid.Parse("b0000001-0000-0000-0000-00000000000d");
    private static readonly Guid NtScan            = Guid.Parse("b0000001-0000-0000-0000-00000000000e");

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefDocumentType>().HasData(
            new { Id = PrenatalCheckup,   Code = "PRENATAL_CHECKUP",    IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Ultrasound,        Code = "ULTRASOUND",          IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = BloodTest,         Code = "BLOOD_TEST",          IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = UrineTest,         Code = "URINE_TEST",          IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Prescription,      Code = "PRESCRIPTION",        IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = VaccinationRecord, Code = "VACCINATION_RECORD",  IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = MedicalReport,     Code = "MEDICAL_REPORT",      IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = Other,             Code = "OTHER",               IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            // Week 5.5: Specific test document types for direct matching with RefTestType
            new { Id = HivTest,           Code = "HIV_TEST",             IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = HepatitisBTest,    Code = "HEPATITIS_B_TEST",     IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = ThyroidTest,       Code = "THYROID_TEST",         IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = GlucoseTest,       Code = "GLUCOSE_TEST",         IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = CbcTest,           Code = "CBC_TEST",             IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate },
            new { Id = NtScan,            Code = "NT_SCAN",              IsActive = true, CreatedAt = SeedDate, UpdatedAt = SeedDate }
        );

        modelBuilder.Entity<RefDocumentTypeTranslation>().HasData(
            // Vietnamese
            new { DocumentTypeId = PrenatalCheckup,   LanguageCode = "vi", DisplayName = "Khám thai",              Description = (string?)"Phiếu khám thai định kỳ" },
            new { DocumentTypeId = Ultrasound,        LanguageCode = "vi", DisplayName = "Siêu âm",                Description = (string?)"Kết quả siêu âm thai" },
            new { DocumentTypeId = BloodTest,         LanguageCode = "vi", DisplayName = "Xét nghiệm máu",         Description = (string?)"Kết quả xét nghiệm máu" },
            new { DocumentTypeId = UrineTest,         LanguageCode = "vi", DisplayName = "Xét nghiệm nước tiểu",   Description = (string?)"Kết quả xét nghiệm nước tiểu" },
            new { DocumentTypeId = Prescription,      LanguageCode = "vi", DisplayName = "Đơn thuốc",              Description = (string?)"Đơn thuốc từ bác sĩ" },
            new { DocumentTypeId = VaccinationRecord, LanguageCode = "vi", DisplayName = "Sổ tiêm chủng",          Description = (string?)"Ghi nhận tiêm chủng" },
            new { DocumentTypeId = MedicalReport,     LanguageCode = "vi", DisplayName = "Báo cáo y tế",           Description = (string?)"Báo cáo y tế tổng hợp" },
            new { DocumentTypeId = Other,             LanguageCode = "vi", DisplayName = "Khác",                    Description = (string?)"Tài liệu y tế khác" },
            new { DocumentTypeId = HivTest,           LanguageCode = "vi", DisplayName = "Xét nghiệm HIV",          Description = (string?)"Kết quả xét nghiệm HIV" },
            new { DocumentTypeId = HepatitisBTest,    LanguageCode = "vi", DisplayName = "Xét nghiệm viêm gan B",   Description = (string?)"Kết quả xét nghiệm viêm gan B (HBsAg)" },
            new { DocumentTypeId = ThyroidTest,       LanguageCode = "vi", DisplayName = "Xét nghiệm tuyến giáp",   Description = (string?)"Kết quả xét nghiệm TSH/tuyến giáp" },
            new { DocumentTypeId = GlucoseTest,       LanguageCode = "vi", DisplayName = "Xét nghiệm đường huyết",  Description = (string?)"Kết quả nghiệm pháp dung nạp glucose (OGTT)" },
            new { DocumentTypeId = CbcTest,           LanguageCode = "vi", DisplayName = "Xét nghiệm công thức máu", Description = (string?)"Kết quả xét nghiệm công thức máu toàn phần (CBC)" },
            new { DocumentTypeId = NtScan,            LanguageCode = "vi", DisplayName = "Đo độ mờ da gáy",         Description = (string?)"Kết quả siêu âm đo độ mờ da gáy (NT scan)" },
            // English
            new { DocumentTypeId = PrenatalCheckup,   LanguageCode = "en", DisplayName = "Prenatal Checkup",        Description = (string?)"Routine prenatal examination report" },
            new { DocumentTypeId = Ultrasound,        LanguageCode = "en", DisplayName = "Ultrasound",              Description = (string?)"Prenatal ultrasound result" },
            new { DocumentTypeId = BloodTest,         LanguageCode = "en", DisplayName = "Blood Test",              Description = (string?)"Blood test result" },
            new { DocumentTypeId = UrineTest,         LanguageCode = "en", DisplayName = "Urine Test",              Description = (string?)"Urine test result" },
            new { DocumentTypeId = Prescription,      LanguageCode = "en", DisplayName = "Prescription",            Description = (string?)"Doctor's prescription" },
            new { DocumentTypeId = VaccinationRecord, LanguageCode = "en", DisplayName = "Vaccination Record",      Description = (string?)"Vaccination record" },
            new { DocumentTypeId = MedicalReport,     LanguageCode = "en", DisplayName = "Medical Report",          Description = (string?)"Comprehensive medical report" },
            new { DocumentTypeId = Other,             LanguageCode = "en", DisplayName = "Other",                   Description = (string?)"Other medical documents" },
            new { DocumentTypeId = HivTest,           LanguageCode = "en", DisplayName = "HIV Test",                Description = (string?)"HIV screening test result" },
            new { DocumentTypeId = HepatitisBTest,    LanguageCode = "en", DisplayName = "Hepatitis B Test",        Description = (string?)"Hepatitis B (HBsAg) test result" },
            new { DocumentTypeId = ThyroidTest,       LanguageCode = "en", DisplayName = "Thyroid Test",            Description = (string?)"TSH/thyroid function test result" },
            new { DocumentTypeId = GlucoseTest,       LanguageCode = "en", DisplayName = "Glucose Test",            Description = (string?)"Oral glucose tolerance test (OGTT) result" },
            new { DocumentTypeId = CbcTest,           LanguageCode = "en", DisplayName = "CBC Test",                Description = (string?)"Complete blood count (CBC) test result" },
            new { DocumentTypeId = NtScan,            LanguageCode = "en", DisplayName = "NT Scan",                 Description = (string?)"Nuchal translucency scan result" }
        );
    }
}
