using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAiPromptTemplateSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ai_model_used",
                table: "ocr_results",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ai_processing_time_ms",
                table: "ocr_results",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ai_prompt_template_id",
                table: "ocr_results",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "ai_tokens_used",
                table: "ocr_results",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ocr_processing_time_ms",
                table: "ocr_results",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_prompt_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    template_key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    display_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    system_rules = table.Column<string>(type: "TEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    domain_rules = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    feature_rules = table.Column<string>(type: "TEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    output_schema = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "gemini-2.5-flash")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    temperature = table.Column<decimal>(type: "DECIMAL(3,2)", nullable: false, defaultValue: 0.1m),
                    max_output_tokens = table.Column<int>(type: "int", nullable: false, defaultValue: 8192),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_prompt_templates", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ai_prompt_templates",
                columns: new[] { "id", "created_at", "deleted_at", "description", "display_name", "domain_rules", "feature_rules", "is_active", "max_output_tokens", "model_name", "output_schema", "system_rules", "temperature", "template_key", "updated_at", "version" },
                values: new object[] { new Guid("a1000001-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Extract structured data from prenatal checkup records (Phiếu Khám Thai MS:51/BV2). Output matches VitalsJsonDto schema for direct storage.", "Medical Record Data Extraction", "VIETNAMESE PRENATAL CARE DOMAIN KNOWLEDGE (Phiếu Khám Thai MS: 51/BV2):\r\n\r\nDocument sections (map to vitalsData fields):\r\nA. Thông tin chung → generalInfo (facility, patient demographics, insurance)\r\nB.I. Lần khám trước → previousVisit (diagnosis, treatment)\r\nB.II. Hỏi bệnh → interview (reason, pregnancy number, gestational age, LMP, expected delivery)\r\nB.III. Tiền sử bệnh → medicalHistory (personal, obstetric, gynecology, family)\r\nB.IV. Khám bệnh → examination (vitalSigns, general, obstetric)\r\nB.VI. Chẩn đoán → diagnosis (text + ICD code)\r\nB.VII. Kế hoạch điều trị → treatmentPlan (medication, health education)\r\nB.VIII. Tiên lượng → prognosis\r\nB.IX. Lần khám kế tiếp → nextAppointment\r\n\r\nCommon metrics and units:\r\n- Mạch (Pulse): lần/phút → pulseBpm (integer)\r\n- Nhiệt độ (Temperature): °C → temperatureCelsius (number)\r\n- Huyết áp / HA: mmHg → bloodPressureSystolic + bloodPressureDiastolic (separate integers)\r\n- Nhịp thở (Respiratory rate): lần/phút → respiratoryRateBpm (integer)\r\n- Cân nặng (Weight): kg → weightKg (number)\r\n- Chiều cao (Height): cm → heightCm (number)\r\n- CCTC / Bề cao tử cung (Fundal height): cm → fundusHeightCm (number)\r\n- Vòng bụng (Abdominal circumference): cm → abdominalCircumferenceCm (number)\r\n- Tim thai / TT / TSM (Fetal heart rate): lần/phút → fetalHeartRateBpm (integer)\r\n- Tuổi thai / Tuần thai: weeks → gestationalWeek (integer, ignore days)\r\n- Protein niệu: g/L or qualitative (+/++/+++) → urineProtein (boolean) + urineProteinValue (number)\r\n\r\nCommon abbreviations:\r\n- TSM: tim sản mạch (fetal heart)\r\n- TC: tử cung (uterus)\r\n- NK: ngôi kiểu (fetal presentation)\r\n- NT: nước tiểu (urine)\r\n- CTG: cardiotocography\r\n- BCTC: bề cao tử cung (fundal height)\r\n- KCC: kinh cuối cùng (last menstrual period)\r\n- PARA: tiền sử sản khoa (obstetric history)\r\n- CTC: cổ tử cung (cervix)\r\n- ÔVN: ối vỡ non (premature rupture of membranes)", "EXTRACTION TASK:\r\nExtract structured medical data from the OCR text of a Vietnamese prenatal checkup form.\r\nFill the 'vitalsData' object following the VitalsJsonDto schema sections:\r\n\r\n1. generalInfo: Patient demographics, facility name, insurance info\r\n2. previousVisit: Previous visit date, diagnosis, treatment (if mentioned)\r\n3. interview: Visit reason, pregnancy number, gestational week, LMP date, expected delivery date\r\n4. medicalHistory: Personal diseases, obstetric history (PARA), gynecology, family history\r\n5. examination.vitalSigns: Pulse, temperature, BP systolic/diastolic (SEPARATE integers), respiratory rate, weight, height\r\n6. examination.general: Mental status, edema, urine protein\r\n7. examination.obstetric: Fundal height, abdominal circumference, fetal presentation, fetal heart rate, cervix, amniotic fluid/sac\r\n8. diagnosis: Diagnosis text + ICD code (if present)\r\n9. treatmentPlan: Medications (as single text), next treatment steps, health education\r\n10. prognosis: normal/risky/cesarean_indicated\r\n11. nextAppointment: Date + notes + examiner type\r\n\r\nAlso fill 'labResults' array for ANY lab/test values found (e.g., blood group, Hb, glucose, HBsAg, HIV, etc.).\r\n\r\nCRITICAL:\r\n- 'bloodPressureSystolic' and 'bloodPressureDiastolic' must be SEPARATE integers. E.g., HA 120/80 → systolic=120, diastolic=80.\r\n- 'gestationalWeek' is an integer (weeks only). E.g., 28T2N → 28.\r\n- Boolean fields: true if mentioned/positive, false if explicitly negative, null if not mentioned.\r\n- Dates: yyyy-MM-dd format.\r\n- If text is partially illegible, extract readable parts and set overallConfidence lower.", true, 8192, "gemini-2.5-flash", "{\r\n  \"vitalsData\": {\r\n    \"generalInfo\": {\r\n      \"facility\": \"string|null\",\r\n      \"managingAuthority\": \"string|null\",\r\n      \"admissionNumber\": \"string|null\",\r\n      \"patientCode\": \"string|null\",\r\n      \"fullName\": \"string|null\",\r\n      \"dateOfBirth\": \"string|null (yyyy-MM-dd)\",\r\n      \"age\": \"integer|null\",\r\n      \"phone\": \"string|null\",\r\n      \"occupation\": \"string|null\",\r\n      \"ethnicity\": \"string|null\",\r\n      \"nationality\": \"string|null\",\r\n      \"address\": \"string|null\",\r\n      \"ward\": \"string|null\",\r\n      \"district\": \"string|null\",\r\n      \"province\": \"string|null\",\r\n      \"insuranceType\": \"string|null (BHYT|thu_phi|mien|khac)\",\r\n      \"insuranceNumber\": \"string|null\",\r\n      \"insuranceExpiry\": \"string|null (yyyy-MM-dd)\",\r\n      \"idNumber\": \"string|null\"\r\n    },\r\n    \"previousVisit\": {\r\n      \"visitDate\": \"string|null (yyyy-MM-dd)\",\r\n      \"diagnosis\": \"string|null\",\r\n      \"treatment\": \"string|null\"\r\n    },\r\n    \"interview\": {\r\n      \"reasonForVisit\": \"string|null\",\r\n      \"pregnancyNumber\": \"integer|null\",\r\n      \"totalVisitCount\": \"integer|null\",\r\n      \"lastMenstrualPeriodDate\": \"string|null (yyyy-MM-dd)\",\r\n      \"gestationalWeek\": \"integer|null\",\r\n      \"expectedDeliveryDate\": \"string|null (yyyy-MM-dd)\",\r\n      \"clinicalProgress\": \"string|null\",\r\n      \"generalCondition\": \"string|null (normal|abnormal)\",\r\n      \"generalConditionNote\": \"string|null\",\r\n      \"tetanusVaccineHistory\": \"integer|null\"\r\n    },\r\n    \"medicalHistory\": {\r\n      \"personal\": {\r\n        \"allergy\": \"boolean|null\",\r\n        \"allergyNote\": \"string|null\",\r\n        \"medicalHistory\": \"boolean|null\",\r\n        \"medicalHistoryNote\": \"string|null\",\r\n        \"hypertension\": \"boolean|null\",\r\n        \"heartDisease\": \"boolean|null\",\r\n        \"respiratoryDisease\": \"boolean|null\",\r\n        \"thyroidDisease\": \"boolean|null\",\r\n        \"kidneyDisease\": \"boolean|null\",\r\n        \"diabetes\": \"boolean|null\",\r\n        \"otherDiseases\": \"string|null\",\r\n        \"currentMedications\": \"boolean|null\",\r\n        \"medicationNote\": \"string|null\",\r\n        \"surgeryHistory\": \"boolean|null\",\r\n        \"surgeryNote\": \"string|null\"\r\n      },\r\n      \"obstetric\": {\r\n        \"para\": \"integer|null\",\r\n        \"previousPregnancies\": [{\"endDate\":\"string|null\",\"gestationalAge\":\"string|null\",\"complicationsDuringPregnancy\":\"string|null\",\"deliveryMethod\":\"string|null\",\"newbornInfo\":\"string|null\",\"postpartum\":\"string|null\"}]\r\n      },\r\n      \"gynecology\": {\r\n        \"menstrualCycle\": \"string|null (regular|irregular)\",\r\n        \"menstrualCycleDays\": \"integer|null\",\r\n        \"gynecologySurgery\": \"boolean|null\",\r\n        \"gynecologySurgeryNote\": \"string|null\",\r\n        \"ovarianTumor\": \"boolean|null\",\r\n        \"uterineFibroid\": \"boolean|null\",\r\n        \"genitalMalformation\": \"boolean|null\",\r\n        \"vaginalInfection\": \"boolean|null\"\r\n      },\r\n      \"pelvicOrganProlapse\": \"boolean|null\",\r\n      \"gynecologicalDiseaseNote\": \"string|null\",\r\n      \"family\": {\r\n        \"hasHistory\": \"boolean|null\",\r\n        \"familyHistoryNote\": \"string|null\",\r\n        \"twins\": \"boolean|null\",\r\n        \"malformation\": \"boolean|null\",\r\n        \"geneticDisease\": \"boolean|null\",\r\n        \"diabetes\": \"boolean|null\",\r\n        \"hypertension\": \"boolean|null\",\r\n        \"otherNote\": \"string|null\"\r\n      }\r\n    },\r\n    \"examination\": {\r\n      \"vitalSigns\": {\r\n        \"pulseBpm\": \"integer|null\",\r\n        \"temperatureCelsius\": \"number|null\",\r\n        \"bloodPressureSystolic\": \"integer|null (mmHg)\",\r\n        \"bloodPressureDiastolic\": \"integer|null (mmHg)\",\r\n        \"respiratoryRateBpm\": \"integer|null\",\r\n        \"weightKg\": \"number|null\",\r\n        \"heightCm\": \"number|null\"\r\n      },\r\n      \"general\": {\r\n        \"mentalStatus\": \"string|null (alert|coma|other)\",\r\n        \"mentalStatusNote\": \"string|null\",\r\n        \"edema\": \"boolean|null\",\r\n        \"urineProtein\": \"boolean|null\",\r\n        \"urineProteinValue\": \"number|null (g/L)\"\r\n      },\r\n      \"obstetric\": {\r\n        \"oldScar\": \"boolean|null\",\r\n        \"scarPainful\": \"boolean|null\",\r\n        \"pelvis\": \"string|null (normal|abnormal)\",\r\n        \"fundusHeightCm\": \"number|null\",\r\n        \"abdominalCircumferenceCm\": \"number|null\",\r\n        \"fetalPresentation\": \"string|null (normal|abnormal)\",\r\n        \"fetalPresentationNote\": \"string|null\",\r\n        \"uterineContraction\": \"boolean|null\",\r\n        \"uterineContractionFrequency\": \"integer|null (per 10 min)\",\r\n        \"fetalHeartbeat\": \"boolean|null\",\r\n        \"fetalHeartRateBpm\": \"integer|null\",\r\n        \"cervix\": \"string|null (closed|effaced|dilated)\",\r\n        \"cervixDilationCm\": \"number|null\",\r\n        \"amnioticSac\": \"string|null (bulging|flat|pear)\",\r\n        \"membraneStatus\": \"string|null (intact|leaking|ruptured)\",\r\n        \"membraneRuptureTime\": \"string|null (HH:mm)\",\r\n        \"amnioticFluid\": \"string|null (clear|green|bloody)\"\r\n      }\r\n    },\r\n    \"diagnosis\": {\r\n      \"text\": \"string|null\",\r\n      \"icdCode\": \"string|null\"\r\n    },\r\n    \"treatmentPlan\": {\r\n      \"medication\": \"string|null\",\r\n      \"nextSteps\": \"string|null\",\r\n      \"healthEducation\": \"boolean|null\",\r\n      \"healthEducationNote\": \"string|null\"\r\n    },\r\n    \"prognosis\": \"string|null (normal|risky|cesarean_indicated)\",\r\n    \"nextAppointment\": {\r\n      \"date\": \"string|null (yyyy-MM-dd)\",\r\n      \"notes\": \"string|null\",\r\n      \"examinerType\": \"string|null (obstetrician|midwife|pediatric_nurse|other)\"\r\n    }\r\n  },\r\n  \"labResults\": [\r\n    {\r\n      \"testName\": \"string\",\r\n      \"value\": \"string|null\",\r\n      \"unit\": \"string|null\",\r\n      \"referenceRange\": \"string|null\",\r\n      \"isAbnormal\": \"boolean|null\"\r\n    }\r\n  ],\r\n  \"overallConfidence\": \"number (0.0-1.0)\"\r\n}", "You are a medical data extraction assistant specializing in Vietnamese prenatal care records (Phiếu Khám Thai, mẫu MS: 51/BV2 — Bộ Y tế Việt Nam).\r\n\r\nRULES:\r\n1. Always respond with valid JSON matching the provided schema EXACTLY. No extra keys, no missing sections.\r\n2. Extract ONLY information explicitly present in the text. Do NOT infer or assume data.\r\n3. If a field is not found in the text, use null (for scalars) or omit from arrays.\r\n4. Do NOT provide medical advice, diagnosis, or interpretations beyond what is written.\r\n5. Preserve original Vietnamese text for names, facilities, addresses, and notes.\r\n6. Convert dates to ISO 8601 format (yyyy-MM-dd) when possible.\r\n7. Convert numeric values to standard units (kg, cm, mmHg, °C, bpm, g/L, mmol/L).\r\n8. Boolean fields: use true/false/null only. Set true if the document explicitly mentions the condition.\r\n9. Flag lab results as abnormal ONLY if the document explicitly states so or provides reference ranges showing out-of-range values.\r\n10. bloodPressureSystolic and bloodPressureDiastolic MUST be SEPARATE integers (e.g., 120 and 80), NOT a combined string.", 0.1m, "medical_record.extraction", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.CreateIndex(
                name: "IX_ocr_results_ai_prompt_template_id",
                table: "ocr_results",
                column: "ai_prompt_template_id");

            migrationBuilder.CreateIndex(
                name: "uk_ai_templates_key_version",
                table: "ai_prompt_templates",
                columns: new[] { "template_key", "version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ocr_results_ai_prompt_templates_ai_prompt_template_id",
                table: "ocr_results",
                column: "ai_prompt_template_id",
                principalTable: "ai_prompt_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ocr_results_ai_prompt_templates_ai_prompt_template_id",
                table: "ocr_results");

            migrationBuilder.DropTable(
                name: "ai_prompt_templates");

            migrationBuilder.DropIndex(
                name: "IX_ocr_results_ai_prompt_template_id",
                table: "ocr_results");

            migrationBuilder.DropColumn(
                name: "ai_model_used",
                table: "ocr_results");

            migrationBuilder.DropColumn(
                name: "ai_processing_time_ms",
                table: "ocr_results");

            migrationBuilder.DropColumn(
                name: "ai_prompt_template_id",
                table: "ocr_results");

            migrationBuilder.DropColumn(
                name: "ai_tokens_used",
                table: "ocr_results");

            migrationBuilder.DropColumn(
                name: "ocr_processing_time_ms",
                table: "ocr_results");
        }
    }
}
