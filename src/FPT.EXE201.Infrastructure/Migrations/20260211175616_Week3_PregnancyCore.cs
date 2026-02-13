using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Week3_PregnancyCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pregnancies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_no = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lmp_date = table.Column<DateTime>(type: "DATE", nullable: true),
                    edd_date = table.Column<DateTime>(type: "DATE", nullable: true),
                    conception_date = table.Column<DateTime>(type: "DATE", nullable: true),
                    current_week = table.Column<int>(type: "int", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    baby_nickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    baby_gender = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "Unknown")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pregnancy_type = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false, defaultValue: "Singleton")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mother_blood_type = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pre_pregnancy_weight_kg = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    height_cm = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    due_date_source = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false, defaultValue: "LMP")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gravida = table.Column<int>(type: "int", nullable: true),
                    para = table.Column<int>(type: "int", nullable: true),
                    actual_delivery_date = table.Column<DateTime>(type: "DATE", nullable: true),
                    delivery_method = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cover_image_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pregnancies", x => x.id);
                    table.ForeignKey(
                        name: "FK_pregnancies_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_pregnancy_conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_pregnancy_conditions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_test_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_test_types", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "prenatal_visits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    doctor_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    visit_at = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    visit_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vitals_json = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prenatal_visits", x => x.id);
                    table.ForeignKey(
                        name: "FK_prenatal_visits_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pregnancy_conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    condition_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    diagnosed_at = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pregnancy_conditions", x => x.id);
                    table.ForeignKey(
                        name: "FK_pregnancy_conditions_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pregnancy_conditions_ref_pregnancy_conditions_condition_id",
                        column: x => x.condition_id,
                        principalTable: "ref_pregnancy_conditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_pregnancy_condition_translations",
                columns: table => new
                {
                    condition_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    lang_code = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_pregnancy_condition_translations", x => new { x.condition_id, x.lang_code });
                    table.ForeignKey(
                        name: "FK_ref_pregnancy_condition_translations_languages_lang_code",
                        column: x => x.lang_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ref_pregnancy_condition_translations_ref_pregnancy_condition~",
                        column: x => x.condition_id,
                        principalTable: "ref_pregnancy_conditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_test_type_translations",
                columns: table => new
                {
                    test_type_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    lang_code = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_test_type_translations", x => new { x.test_type_id, x.lang_code });
                    table.ForeignKey(
                        name: "FK_ref_test_type_translations_languages_lang_code",
                        column: x => x.lang_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ref_test_type_translations_ref_test_types_test_type_id",
                        column: x => x.test_type_id,
                        principalTable: "ref_test_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "prenatal_tests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    visit_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    test_type_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    test_at = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    result_text = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    result_json = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    abnormal_flag = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prenatal_tests", x => x.id);
                    table.ForeignKey(
                        name: "FK_prenatal_tests_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prenatal_tests_prenatal_visits_visit_id",
                        column: x => x.visit_id,
                        principalTable: "prenatal_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_prenatal_tests_ref_test_types_test_type_id",
                        column: x => x.test_type_id,
                        principalTable: "ref_test_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ref_pregnancy_conditions",
                columns: new[] { "id", "code", "created_at", "deleted_at", "is_active", "updated_at" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000001"), "GESTATIONAL_DIABETES", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000002"), "PREECLAMPSIA", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000003"), "ANEMIA", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000004"), "HYPEREMESIS_GRAVIDARUM", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000005"), "PLACENTA_PREVIA", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000006"), "HYPERTENSION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000007"), "THYROID_DISORDER", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000008"), "GROUP_B_STREP", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000009"), "CERVICAL_INSUFFICIENCY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-00000000000a"), "ECTOPIC_PREGNANCY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ref_test_types",
                columns: new[] { "id", "category", "code", "created_at", "deleted_at", "is_active", "updated_at" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000001"), "LAB", "BLOOD_GLUCOSE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000002"), "IMAGING", "ULTRASOUND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000003"), "OTHER", "BLOOD_PRESSURE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000004"), "LAB", "COMPLETE_BLOOD_COUNT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000005"), "LAB", "URINE_TEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000006"), "LAB", "HEPATITIS_B", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000007"), "LAB", "HIV_SCREEN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000008"), "LAB", "TSH", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-000000000009"), "IMAGING", "NT_SCAN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000001-0000-0000-0000-00000000000a"), "LAB", "OGTT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ref_pregnancy_condition_translations",
                columns: new[] { "condition_id", "lang_code", "description", "name" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000001"), "en", "High blood sugar that develops during pregnancy", "Gestational Diabetes" },
                    { new Guid("a0000001-0000-0000-0000-000000000001"), "vi", "Tình trạng đường huyết cao phát triển trong thai kỳ", "Tiểu đường thai kỳ" },
                    { new Guid("a0000001-0000-0000-0000-000000000002"), "en", "High blood pressure and protein in urine after 20 weeks", "Preeclampsia" },
                    { new Guid("a0000001-0000-0000-0000-000000000002"), "vi", "Huyết áp cao và protein niệu sau tuần 20", "Tiền sản giật" },
                    { new Guid("a0000001-0000-0000-0000-000000000003"), "en", "Low red blood cell count or hemoglobin", "Anemia" },
                    { new Guid("a0000001-0000-0000-0000-000000000003"), "vi", "Lượng hồng cầu hoặc hemoglobin thấp", "Thiếu máu" },
                    { new Guid("a0000001-0000-0000-0000-000000000004"), "en", "Severe nausea and vomiting during pregnancy", "Hyperemesis Gravidarum" },
                    { new Guid("a0000001-0000-0000-0000-000000000004"), "vi", "Buồn nôn và nôn nghiêm trọng trong thai kỳ", "Nghén nặng" },
                    { new Guid("a0000001-0000-0000-0000-000000000005"), "en", "Placenta covers the cervix", "Placenta Previa" },
                    { new Guid("a0000001-0000-0000-0000-000000000005"), "vi", "Nhau thai che phủ cổ tử cung", "Nhau tiền đạo" },
                    { new Guid("a0000001-0000-0000-0000-000000000006"), "en", "High blood pressure after week 20 without proteinuria", "Gestational Hypertension" },
                    { new Guid("a0000001-0000-0000-0000-000000000006"), "vi", "Huyết áp cao phát hiện sau tuần 20", "Tăng huyết áp thai kỳ" },
                    { new Guid("a0000001-0000-0000-0000-000000000007"), "en", "Hyperthyroidism or hypothyroidism during pregnancy", "Thyroid Disorder" },
                    { new Guid("a0000001-0000-0000-0000-000000000007"), "vi", "Cường giáp hoặc suy giáp trong thai kỳ", "Rối loạn tuyến giáp" },
                    { new Guid("a0000001-0000-0000-0000-000000000008"), "en", "GBS bacteria that may pass to baby during delivery", "Group B Streptococcus" },
                    { new Guid("a0000001-0000-0000-0000-000000000008"), "vi", "Vi khuẩn GBS có thể lây sang con khi sinh", "Nhiễm liên cầu nhóm B" },
                    { new Guid("a0000001-0000-0000-0000-000000000009"), "en", "Cervix opens prematurely, risk of preterm birth", "Cervical Insufficiency" },
                    { new Guid("a0000001-0000-0000-0000-000000000009"), "vi", "Cổ tử cung mở sớm, nguy cơ sinh non", "Hở eo cổ tử cung" },
                    { new Guid("a0000001-0000-0000-0000-00000000000a"), "en", "Pregnancy implanted outside the uterus", "Ectopic Pregnancy" },
                    { new Guid("a0000001-0000-0000-0000-00000000000a"), "vi", "Thai làm tổ ngoài buồng tử cung", "Thai ngoài tử cung" }
                });

            migrationBuilder.InsertData(
                table: "ref_test_type_translations",
                columns: new[] { "lang_code", "test_type_id", "description", "name" },
                values: new object[,]
                {
                    { "en", new Guid("b0000001-0000-0000-0000-000000000001"), "Measures glucose level in blood", "Blood Glucose Test" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000001"), "Kiểm tra nồng độ glucose trong máu", "Xét nghiệm đường huyết" },
                    { "en", new Guid("b0000001-0000-0000-0000-000000000002"), "Imaging of fetus using sound waves", "Ultrasound" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000002"), "Chụp hình ảnh thai nhi bằng sóng siêu âm", "Siêu âm" },
                    { "en", new Guid("b0000001-0000-0000-0000-000000000003"), "Measures blood pressure in arteries", "Blood Pressure" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000003"), "Đo áp lực máu trong động mạch", "Đo huyết áp" },
                    { "en", new Guid("b0000001-0000-0000-0000-000000000004"), "Counts different blood cell types", "Complete Blood Count" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000004"), "Đếm số lượng và phân loại tế bào máu", "Công thức máu toàn phần" },
                    { "en", new Guid("b0000001-0000-0000-0000-000000000005"), "Analyzes urine composition", "Urine Test" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000005"), "Phân tích thành phần nước tiểu", "Xét nghiệm nước tiểu" },
                    { "en", new Guid("b0000001-0000-0000-0000-000000000006"), "Screens for hepatitis B virus", "Hepatitis B Screen" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000006"), "Tầm soát virus viêm gan B", "Xét nghiệm viêm gan B" },
                    { "en", new Guid("b0000001-0000-0000-0000-000000000007"), "Screens for HIV virus", "HIV Screen" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000007"), "Tầm soát virus HIV", "Xét nghiệm HIV" },
                    { "en", new Guid("b0000001-0000-0000-0000-000000000008"), "Checks thyroid function", "TSH Test" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000008"), "Kiểm tra chức năng tuyến giáp", "Xét nghiệm TSH" },
                    { "en", new Guid("b0000001-0000-0000-0000-000000000009"), "Ultrasound screening for fetal abnormalities", "Nuchal Translucency Scan" },
                    { "vi", new Guid("b0000001-0000-0000-0000-000000000009"), "Siêu âm tầm soát dị tật thai nhi", "Đo độ mờ da gáy" },
                    { "en", new Guid("b0000001-0000-0000-0000-00000000000a"), "Diagnostic test for gestational diabetes", "Oral Glucose Tolerance Test" },
                    { "vi", new Guid("b0000001-0000-0000-0000-00000000000a"), "Xét nghiệm chẩn đoán tiểu đường thai kỳ", "Nghiệm pháp dung nạp glucose" }
                });

            migrationBuilder.CreateIndex(
                name: "idx_pregnancies_status",
                table: "pregnancies",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_pregnancies_user",
                table: "pregnancies",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uk_pregnancies_user_no",
                table: "pregnancies",
                columns: new[] { "user_id", "pregnancy_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pregnancy_conditions_condition",
                table: "pregnancy_conditions",
                column: "condition_id");

            migrationBuilder.CreateIndex(
                name: "idx_pregnancy_conditions_pregnancy",
                table: "pregnancy_conditions",
                column: "pregnancy_id");

            migrationBuilder.CreateIndex(
                name: "uk_pregnancy_condition",
                table: "pregnancy_conditions",
                columns: new[] { "pregnancy_id", "condition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_tests_date",
                table: "prenatal_tests",
                column: "test_at");

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_tests_pregnancy",
                table: "prenatal_tests",
                column: "pregnancy_id");

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_tests_visit",
                table: "prenatal_tests",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "IX_prenatal_tests_test_type_id",
                table: "prenatal_tests",
                column: "test_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_visits_date",
                table: "prenatal_visits",
                column: "visit_at");

            migrationBuilder.CreateIndex(
                name: "idx_prenatal_visits_pregnancy",
                table: "prenatal_visits",
                column: "pregnancy_id");

            migrationBuilder.CreateIndex(
                name: "IX_ref_pregnancy_condition_translations_lang_code",
                table: "ref_pregnancy_condition_translations",
                column: "lang_code");

            migrationBuilder.CreateIndex(
                name: "uk_ref_conditions_code",
                table: "ref_pregnancy_conditions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ref_test_type_translations_lang_code",
                table: "ref_test_type_translations",
                column: "lang_code");

            migrationBuilder.CreateIndex(
                name: "idx_ref_test_types_category",
                table: "ref_test_types",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "uk_ref_test_types_code",
                table: "ref_test_types",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pregnancy_conditions");

            migrationBuilder.DropTable(
                name: "prenatal_tests");

            migrationBuilder.DropTable(
                name: "ref_pregnancy_condition_translations");

            migrationBuilder.DropTable(
                name: "ref_test_type_translations");

            migrationBuilder.DropTable(
                name: "prenatal_visits");

            migrationBuilder.DropTable(
                name: "ref_pregnancy_conditions");

            migrationBuilder.DropTable(
                name: "ref_test_types");

            migrationBuilder.DropTable(
                name: "pregnancies");
        }
    }
}
