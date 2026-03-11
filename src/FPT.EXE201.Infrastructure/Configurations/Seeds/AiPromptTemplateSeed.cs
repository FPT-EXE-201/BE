using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations.Seeds;

public class AiPromptTemplateSeed : IEntityTypeConfiguration<AiPromptTemplate>
{
    public void Configure(EntityTypeBuilder<AiPromptTemplate> builder)
    {
        builder.HasData(
            new
            {
                Id = Guid.Parse("a1000001-0000-0000-0000-000000000001"),
                TemplateKey = "medical_record.extraction",
                Version = 1,
                DisplayName = "Medical Record Data Extraction",
                Description = "Extract structured data from prenatal checkup records (Phiếu Khám Thai MS:51/BV2). Output matches VitalsJsonDto schema for direct storage.",

                SystemRules = @"You are a medical data extraction assistant specializing in Vietnamese prenatal care records (Phiếu Khám Thai, mẫu MS: 51/BV2 — Bộ Y tế Việt Nam).

RULES:
1. Always respond with valid JSON matching the provided schema EXACTLY. No extra keys, no missing sections.
2. Extract ONLY information explicitly present in the text. Do NOT infer or assume data.
3. If a field is not found in the text, use null (for scalars) or omit from arrays.
4. Do NOT provide medical advice, diagnosis, or interpretations beyond what is written.
5. Preserve original Vietnamese text for names, facilities, addresses, and notes.
6. Convert dates to ISO 8601 format (yyyy-MM-dd) when possible.
7. Convert numeric values to standard units (kg, cm, mmHg, °C, bpm, g/L, mmol/L).
8. Boolean fields: use true/false/null only. Set true if the document explicitly mentions the condition.
9. Flag lab results as abnormal ONLY if the document explicitly states so or provides reference ranges showing out-of-range values.
10. bloodPressureSystolic and bloodPressureDiastolic MUST be SEPARATE integers (e.g., 120 and 80), NOT a combined string.",

                DomainRules = @"VIETNAMESE PRENATAL CARE DOMAIN KNOWLEDGE (Phiếu Khám Thai MS: 51/BV2):

Document sections (map to vitalsData fields):
A. Thông tin chung → generalInfo (facility, patient demographics, insurance)
B.I. Lần khám trước → previousVisit (diagnosis, treatment)
B.II. Hỏi bệnh → interview (reason, pregnancy number, gestational age, LMP, expected delivery)
B.III. Tiền sử bệnh → medicalHistory (personal, obstetric, gynecology, family)
B.IV. Khám bệnh → examination (vitalSigns, general, obstetric)
B.VI. Chẩn đoán → diagnosis (text + ICD code)
B.VII. Kế hoạch điều trị → treatmentPlan (medication, health education)
B.VIII. Tiên lượng → prognosis
B.IX. Lần khám kế tiếp → nextAppointment

Common metrics and units:
- Mạch (Pulse): lần/phút → pulseBpm (integer)
- Nhiệt độ (Temperature): °C → temperatureCelsius (number)
- Huyết áp / HA: mmHg → bloodPressureSystolic + bloodPressureDiastolic (separate integers)
- Nhịp thở (Respiratory rate): lần/phút → respiratoryRateBpm (integer)
- Cân nặng (Weight): kg → weightKg (number)
- Chiều cao (Height): cm → heightCm (number)
- CCTC / Bề cao tử cung (Fundal height): cm → fundusHeightCm (number)
- Vòng bụng (Abdominal circumference): cm → abdominalCircumferenceCm (number)
- Tim thai / TT / TSM (Fetal heart rate): lần/phút → fetalHeartRateBpm (integer)
- Tuổi thai / Tuần thai: weeks → gestationalWeek (integer, ignore days)
- Protein niệu: g/L or qualitative (+/++/+++) → urineProtein (boolean) + urineProteinValue (number)

Common abbreviations:
- TSM: tim sản mạch (fetal heart)
- TC: tử cung (uterus)
- NK: ngôi kiểu (fetal presentation)
- NT: nước tiểu (urine)
- CTG: cardiotocography
- BCTC: bề cao tử cung (fundal height)
- KCC: kinh cuối cùng (last menstrual period)
- PARA: tiền sử sản khoa (obstetric history)
- CTC: cổ tử cung (cervix)
- ÔVN: ối vỡ non (premature rupture of membranes)",

                FeatureRules = @"EXTRACTION TASK:
Extract structured medical data from the OCR text of a Vietnamese prenatal checkup form.
Fill the 'vitalsData' object following the VitalsJsonDto schema sections:

1. generalInfo: Patient demographics, facility name, insurance info
2. previousVisit: Previous visit date, diagnosis, treatment (if mentioned)
3. interview: Visit reason, pregnancy number, gestational week, LMP date, expected delivery date
4. medicalHistory: Personal diseases, obstetric history (PARA), gynecology, family history
5. examination.vitalSigns: Pulse, temperature, BP systolic/diastolic (SEPARATE integers), respiratory rate, weight, height
6. examination.general: Mental status, edema, urine protein
7. examination.obstetric: Fundal height, abdominal circumference, fetal presentation, fetal heart rate, cervix, amniotic fluid/sac
8. diagnosis: Diagnosis text + ICD code (if present)
9. treatmentPlan: Medications (as single text), next treatment steps, health education
10. prognosis: normal/risky/cesarean_indicated
11. nextAppointment: Date + notes + examiner type

CRITICAL:
- 'bloodPressureSystolic' and 'bloodPressureDiastolic' must be SEPARATE integers. E.g., HA 120/80 → systolic=120, diastolic=80.
- 'gestationalWeek' is an integer (weeks only). E.g., 28T2N → 28.
- Boolean fields: true if mentioned/positive, false if explicitly negative, null if not mentioned.
- Dates: yyyy-MM-dd format.
- If text is partially illegible, extract readable parts and set overallConfidence lower.",

                OutputSchema = @"{
  ""vitalsData"": {
    ""generalInfo"": {
      ""facility"": ""string|null"",
      ""managingAuthority"": ""string|null"",
      ""admissionNumber"": ""string|null"",
      ""patientCode"": ""string|null"",
      ""fullName"": ""string|null"",
      ""dateOfBirth"": ""string|null (yyyy-MM-dd)"",
      ""age"": ""integer|null"",
      ""phone"": ""string|null"",
      ""occupation"": ""string|null"",
      ""ethnicity"": ""string|null"",
      ""nationality"": ""string|null"",
      ""address"": ""string|null"",
      ""ward"": ""string|null"",
      ""district"": ""string|null"",
      ""province"": ""string|null"",
      ""insuranceType"": ""string|null (BHYT|thu_phi|mien|khac)"",
      ""insuranceNumber"": ""string|null"",
      ""insuranceExpiry"": ""string|null (yyyy-MM-dd)"",
      ""idNumber"": ""string|null""
    },
    ""previousVisit"": {
      ""visitDate"": ""string|null (yyyy-MM-dd)"",
      ""diagnosis"": ""string|null"",
      ""treatment"": ""string|null""
    },
    ""interview"": {
      ""reasonForVisit"": ""string|null"",
      ""pregnancyNumber"": ""integer|null"",
      ""totalVisitCount"": ""integer|null"",
      ""lastMenstrualPeriodDate"": ""string|null (yyyy-MM-dd)"",
      ""gestationalWeek"": ""integer|null"",
      ""expectedDeliveryDate"": ""string|null (yyyy-MM-dd)"",
      ""clinicalProgress"": ""string|null"",
      ""generalCondition"": ""string|null (normal|abnormal)"",
      ""generalConditionNote"": ""string|null"",
      ""tetanusVaccineHistory"": ""integer|null""
    },
    ""medicalHistory"": {
      ""personal"": {
        ""allergy"": ""boolean|null"",
        ""allergyNote"": ""string|null"",
        ""medicalHistory"": ""boolean|null"",
        ""medicalHistoryNote"": ""string|null"",
        ""hypertension"": ""boolean|null"",
        ""heartDisease"": ""boolean|null"",
        ""respiratoryDisease"": ""boolean|null"",
        ""thyroidDisease"": ""boolean|null"",
        ""kidneyDisease"": ""boolean|null"",
        ""diabetes"": ""boolean|null"",
        ""otherDiseases"": ""string|null"",
        ""currentMedications"": ""boolean|null"",
        ""medicationNote"": ""string|null"",
        ""surgeryHistory"": ""boolean|null"",
        ""surgeryNote"": ""string|null""
      },
      ""obstetric"": {
        ""para"": ""integer|null"",
        ""previousPregnancies"": [{""endDate"":""string|null"",""gestationalAge"":""string|null"",""complicationsDuringPregnancy"":""string|null"",""deliveryMethod"":""string|null"",""newbornInfo"":""string|null"",""postpartum"":""string|null""}]
      },
      ""gynecology"": {
        ""menstrualCycle"": ""string|null (regular|irregular)"",
        ""menstrualCycleDays"": ""integer|null"",
        ""gynecologySurgery"": ""boolean|null"",
        ""gynecologySurgeryNote"": ""string|null"",
        ""ovarianTumor"": ""boolean|null"",
        ""uterineFibroid"": ""boolean|null"",
        ""genitalMalformation"": ""boolean|null"",
        ""vaginalInfection"": ""boolean|null""
      },
      ""pelvicOrganProlapse"": ""boolean|null"",
      ""gynecologicalDiseaseNote"": ""string|null"",
      ""family"": {
        ""hasHistory"": ""boolean|null"",
        ""familyHistoryNote"": ""string|null"",
        ""twins"": ""boolean|null"",
        ""malformation"": ""boolean|null"",
        ""geneticDisease"": ""boolean|null"",
        ""diabetes"": ""boolean|null"",
        ""hypertension"": ""boolean|null"",
        ""otherNote"": ""string|null""
      }
    },
    ""examination"": {
      ""vitalSigns"": {
        ""pulseBpm"": ""integer|null"",
        ""temperatureCelsius"": ""number|null"",
        ""bloodPressureSystolic"": ""integer|null (mmHg)"",
        ""bloodPressureDiastolic"": ""integer|null (mmHg)"",
        ""respiratoryRateBpm"": ""integer|null"",
        ""weightKg"": ""number|null"",
        ""heightCm"": ""number|null""
      },
      ""general"": {
        ""mentalStatus"": ""string|null (alert|coma|other)"",
        ""mentalStatusNote"": ""string|null"",
        ""edema"": ""boolean|null"",
        ""urineProtein"": ""boolean|null"",
        ""urineProteinValue"": ""number|null (g/L)""
      },
      ""obstetric"": {
        ""oldScar"": ""boolean|null"",
        ""scarPainful"": ""boolean|null"",
        ""pelvis"": ""string|null (normal|abnormal)"",
        ""fundusHeightCm"": ""number|null"",
        ""abdominalCircumferenceCm"": ""number|null"",
        ""fetalPresentation"": ""string|null (normal|abnormal)"",
        ""fetalPresentationNote"": ""string|null"",
        ""uterineContraction"": ""boolean|null"",
        ""uterineContractionFrequency"": ""integer|null (per 10 min)"",
        ""fetalHeartbeat"": ""boolean|null"",
        ""fetalHeartRateBpm"": ""integer|null"",
        ""cervix"": ""string|null (closed|effaced|dilated)"",
        ""cervixDilationCm"": ""number|null"",
        ""amnioticSac"": ""string|null (bulging|flat|pear)"",
        ""membraneStatus"": ""string|null (intact|leaking|ruptured)"",
        ""membraneRuptureTime"": ""string|null (HH:mm)"",
        ""amnioticFluid"": ""string|null (clear|green|bloody)""
      }
    },
    ""diagnosis"": {
      ""text"": ""string|null"",
      ""icdCode"": ""string|null""
    },
    ""treatmentPlan"": {
      ""medication"": ""string|null"",
      ""nextSteps"": ""string|null"",
      ""healthEducation"": ""boolean|null"",
      ""healthEducationNote"": ""string|null""
    },
    ""prognosis"": ""string|null (normal|risky|cesarean_indicated)"",
    ""nextAppointment"": {
      ""date"": ""string|null (yyyy-MM-dd)"",
      ""notes"": ""string|null"",
      ""examinerType"": ""string|null (obstetrician|midwife|pediatric_nurse|other)""
    }
  },
  ""overallConfidence"": ""number (0.0-1.0)""
}",

                ModelName = "gemini-2.5-flash",
                Temperature = 0.1,
                MaxOutputTokens = 16384,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Week 7 — Nutrition Meal Plan Template
        builder.HasData(
            new
            {
                Id = Guid.Parse("a1000002-0000-0000-0000-000000000001"),
                TemplateKey = "nutrition.meal_plan",
                Version = 1,
                DisplayName = "Nutrition Meal Plan Generator",
                Description = "Generate 7-day AI meal plans with Vietnamese dishes, recipes, and nutrients for pregnant women.",

                SystemRules = @"You are a certified prenatal nutritionist AI assistant.
Respond in Vietnamese.
Output ONLY valid JSON matching the provided schema.
No markdown, no explanation, no extra text outside JSON.",

                DomainRules = @"Pregnancy nutrition guidelines (IOM):
- Trimester 1 (week 1-12): Focus folic acid (600mcg/day), no extra calories.
- Trimester 2 (week 13-26): +340 kcal/day, iron (27mg/day), calcium (1000mg/day).
- Trimester 3 (week 27-40): +450 kcal/day, increase protein, DHA.
- Daily water: 2.3L minimum.
- Avoid: raw fish, high-mercury fish, unpasteurized dairy, alcohol.
- Gestational diabetes: low GI foods, split meals, limit sugar.
- Preeclampsia: reduce sodium, increase potassium.",

                FeatureRules = @"Generate a 7-day meal plan with exactly 4 meals per day: BREAKFAST, LUNCH, DINNER, SNACK.

For EVERY meal item, you MUST provide:
- itemName: Vietnamese dish name (concise)
- portionText: serving size in Vietnamese
- caloriesKcal: integer
- notes: brief nutrition note in Vietnamese (nullable)
- recipe: REQUIRED object with:
  - title: dish name
  - instructions: step-by-step cooking instructions in Vietnamese
  - servings: integer
  - prepMinutes: integer
  - cookMinutes: integer
- nutrients: array of objects, ONLY use these codes:
  PROTEIN, CARBOHYDRATES, FAT, FIBER, IRON, CALCIUM,
  FOLIC_ACID, VITAMIN_D, VITAMIN_C, VITAMIN_A,
  VITAMIN_B12, OMEGA_3, DHA, ZINC
  Each: { ""code"": ""PROTEIN"", ""amount"": 12.5 }

Ensure variety: do not repeat the same dish within 3 days.
Each day's total calories should be close to {targetCalories} kcal.",

                OutputSchema = @"{
  ""title"": ""string"",
  ""totalDailyCalories"": ""number"",
  ""notes"": ""string"",
  ""days"": [
    {
      ""date"": ""YYYY-MM-DD"",
      ""meals"": [
        {
          ""mealType"": ""BREAKFAST|LUNCH|DINNER|SNACK"",
          ""itemName"": ""string"",
          ""portionText"": ""string"",
          ""caloriesKcal"": ""number"",
          ""notes"": ""string|null"",
          ""recipe"": {
            ""title"": ""string"",
            ""instructions"": ""string"",
            ""servings"": ""number"",
            ""prepMinutes"": ""number"",
            ""cookMinutes"": ""number""
          },
          ""nutrients"": [
            { ""code"": ""string"", ""amount"": ""number"" }
          ]
        }
      ]
    }
  ]
}",

                ModelName = "gemini-2.5-flash",
                Temperature = 0.7,
                MaxOutputTokens = 16384,
                IsActive = true,
                CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
