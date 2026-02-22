using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Week6_WeightTrackingMotivational : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "motivational_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    category = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    week_start = table.Column<int>(type: "int", nullable: false),
                    week_end = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    variables_json = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motivational_templates", x => x.id);
                    table.CheckConstraint("chk_motivational_week", "week_start >= 0 AND week_end >= week_start AND week_end <= 45");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "weight_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    alert_type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    triggered_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    details_json = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resolved_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weight_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_weight_alerts_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "weight_goal_ranges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    height_cm = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    pre_pregnancy_weight_kg = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    bmi = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    recommended_total_gain_min = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    recommended_total_gain_max = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weight_goal_ranges", x => x.id);
                    table.ForeignKey(
                        name: "FK_weight_goal_ranges_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "weight_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    logged_on = table.Column<DateOnly>(type: "DATE", nullable: false),
                    weight_kg = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weight_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_weight_logs_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "motivational_template_translations",
                columns: table => new
                {
                    template_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    language_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motivational_template_translations", x => new { x.template_id, x.language_code });
                    table.ForeignKey(
                        name: "FK_motivational_template_translations_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_motivational_template_translations_motivational_templates_te~",
                        column: x => x.template_id,
                        principalTable: "motivational_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "motivational_templates",
                columns: new[] { "id", "category", "created_at", "deleted_at", "is_active", "updated_at", "variables_json", "week_end", "week_start" },
                values: new object[,]
                {
                    { new Guid("c6000001-0000-0000-0000-000000000001"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"hạt mè\",\"fruitEn\":\"poppy seed\",\"sizeCm\":\"0.1\"}", 5, 4 },
                    { new Guid("c6000001-0000-0000-0000-000000000002"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"hạt đậu lăng\",\"fruitEn\":\"lentil\",\"sizeCm\":\"0.6\"}", 7, 6 },
                    { new Guid("c6000001-0000-0000-0000-000000000003"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả mâm xôi\",\"fruitEn\":\"raspberry\",\"sizeCm\":\"1.6\"}", 9, 8 },
                    { new Guid("c6000001-0000-0000-0000-000000000004"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả mận\",\"fruitEn\":\"prune\",\"sizeCm\":\"3.1\"}", 11, 10 },
                    { new Guid("c6000001-0000-0000-0000-000000000005"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả chanh\",\"fruitEn\":\"lime\",\"sizeCm\":\"5.4\"}", 13, 12 },
                    { new Guid("c6000001-0000-0000-0000-000000000006"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả cam\",\"fruitEn\":\"orange\",\"sizeCm\":\"8.7\"}", 15, 14 },
                    { new Guid("c6000001-0000-0000-0000-000000000007"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả bơ\",\"fruitEn\":\"avocado\",\"sizeCm\":\"11.6\"}", 17, 16 },
                    { new Guid("c6000001-0000-0000-0000-000000000008"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả xoài\",\"fruitEn\":\"mango\",\"sizeCm\":\"15.3\"}", 19, 18 },
                    { new Guid("c6000001-0000-0000-0000-000000000009"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả chuối\",\"fruitEn\":\"banana\",\"sizeCm\":\"25.6\"}", 21, 20 },
                    { new Guid("c6000001-0000-0000-0000-00000000000a"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả bắp\",\"fruitEn\":\"corn\",\"sizeCm\":\"28.9\"}", 23, 22 },
                    { new Guid("c6000001-0000-0000-0000-00000000000b"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả dưa lưới\",\"fruitEn\":\"cantaloupe\",\"sizeCm\":\"30.0\"}", 25, 24 },
                    { new Guid("c6000001-0000-0000-0000-00000000000c"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"bông cải xanh\",\"fruitEn\":\"broccoli\",\"sizeCm\":\"36.6\"}", 27, 26 },
                    { new Guid("c6000001-0000-0000-0000-00000000000d"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả bí ngô\",\"fruitEn\":\"butternut squash\",\"sizeCm\":\"38.6\"}", 29, 28 },
                    { new Guid("c6000001-0000-0000-0000-00000000000e"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả dừa\",\"fruitEn\":\"coconut\",\"sizeCm\":\"40.0\"}", 31, 30 },
                    { new Guid("c6000001-0000-0000-0000-00000000000f"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả dứa\",\"fruitEn\":\"pineapple\",\"sizeCm\":\"42.4\"}", 33, 32 },
                    { new Guid("c6000001-0000-0000-0000-000000000010"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả dưa hấu\",\"fruitEn\":\"honeydew melon\",\"sizeCm\":\"45.0\"}", 35, 34 },
                    { new Guid("c6000001-0000-0000-0000-000000000011"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả bưởi\",\"fruitEn\":\"papaya\",\"sizeCm\":\"47.4\"}", 37, 36 },
                    { new Guid("c6000001-0000-0000-0000-000000000012"), "BabySize", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"fruitVi\":\"quả dưa hấu\",\"fruitEn\":\"watermelon\",\"sizeCm\":\"50.0\"}", 40, 38 },
                    { new Guid("c6000002-0000-0000-0000-000000000001"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 9, 8 },
                    { new Guid("c6000002-0000-0000-0000-000000000002"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 13, 12 },
                    { new Guid("c6000002-0000-0000-0000-000000000003"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 17, 16 },
                    { new Guid("c6000002-0000-0000-0000-000000000004"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 21, 20 },
                    { new Guid("c6000002-0000-0000-0000-000000000005"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 25, 24 },
                    { new Guid("c6000002-0000-0000-0000-000000000006"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 29, 28 },
                    { new Guid("c6000002-0000-0000-0000-000000000007"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 33, 32 },
                    { new Guid("c6000002-0000-0000-0000-000000000008"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 37, 36 },
                    { new Guid("c6000002-0000-0000-0000-000000000009"), "Milestone", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 40, 38 },
                    { new Guid("c6000003-0000-0000-0000-000000000001"), "Tip", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 0 },
                    { new Guid("c6000003-0000-0000-0000-000000000002"), "Tip", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 27, 13 },
                    { new Guid("c6000003-0000-0000-0000-000000000003"), "Tip", new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, 40, 28 }
                });

            migrationBuilder.InsertData(
                table: "motivational_template_translations",
                columns: new[] { "language_code", "template_id", "message", "title" },
                values: new object[,]
                {
                    { "en", new Guid("c6000001-0000-0000-0000-000000000001"), "Week 4-5: Baby is just 0.1 cm, but organs are starting to form. Remember to take your folic acid!", "Baby is the size of a poppy seed!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000001"), "Tuần 4-5: Bé mới chỉ nhỏ bằng hạt mè (0.1 cm), nhưng các cơ quan đã bắt đầu hình thành. Hãy bổ sung acid folic nhé mẹ!", "Bé to bằng hạt mè!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000002"), "Week 6-7: Baby is about 0.6 cm and the heart has started beating. You may see the heartbeat on ultrasound!", "Baby is the size of a lentil!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000002"), "Tuần 6-7: Bé dài khoảng 0.6 cm, tim bé đã bắt đầu đập. Mẹ có thể thấy nhịp tim bé qua siêu âm!", "Bé to bằng hạt đậu lăng!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000003"), "Week 8-9: Baby is 1.6 cm, fingers are forming. Stay hydrated!", "Baby is the size of a raspberry!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000003"), "Tuần 8-9: Bé dài 1.6 cm, các ngón tay bé đang hình thành. Mẹ nhớ uống đủ nước nhé!", "Bé to bằng quả mâm xôi!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000004"), "Week 10-11: Baby is 3.1 cm and can make small movements. Morning sickness may peak around now.", "Baby is the size of a prune!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000004"), "Tuần 10-11: Bé dài 3.1 cm, đã có thể cử động nhẹ. Giai đoạn này mẹ có thể bị ốm nghén nhiều.", "Bé to bằng quả mận!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000005"), "Week 12-13: Baby is 5.4 cm with more defined facial features. Morning sickness should ease soon!", "Baby is the size of a lime!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000005"), "Tuần 12-13: Bé dài 5.4 cm, khuôn mặt bé đã rõ nét hơn. Mẹ sắp qua giai đoạn ốm nghén rồi!", "Bé to bằng quả chanh!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000006"), "Week 14-15: Baby is 8.7 cm — can squint, frown, and suck thumb. You should feel more energetic!", "Baby is the size of an orange!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000006"), "Tuần 14-15: Bé dài 8.7 cm, bé đã biết nhăn mặt và mút tay. Mẹ bắt đầu cảm thấy khỏe hơn!", "Bé to bằng quả cam!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000007"), "Week 16-17: Baby is 11.6 cm, bones are hardening. You may start feeling first kicks!", "Baby is the size of an avocado!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000007"), "Tuần 16-17: Bé dài 11.6 cm, xương bé đang cứng dần. Mẹ có thể bắt đầu cảm nhận bé đạp nhẹ!", "Bé to bằng quả bơ!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000008"), "Week 18-19: Baby is 15.3 cm and can hear sounds. Talk to your baby every day!", "Baby is the size of a mango!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000008"), "Tuần 18-19: Bé dài 15.3 cm, bé đã biết nghe âm thanh. Hãy nói chuyện với bé mỗi ngày nhé!", "Bé to bằng quả xoài!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000009"), "Week 20-21: Baby is 25.6 cm — halfway there! Baby now has eyebrows and eyelids.", "Baby is the size of a banana!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000009"), "Tuần 20-21: Bé dài 25.6 cm — nửa chặng đường rồi mẹ ơi! Bé đã có lông mày và mi mắt.", "Bé to bằng quả chuối!" },
                    { "en", new Guid("c6000001-0000-0000-0000-00000000000a"), "Week 22-23: Baby is 28.9 cm, skin is becoming more opaque. Remember to take your iron supplements!", "Baby is the size of an ear of corn!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-00000000000a"), "Tuần 22-23: Bé dài 28.9 cm, da bé đang dần hồng hào hơn. Mẹ nhớ bổ sung sắt nhé!", "Bé to bằng bắp ngô!" },
                    { "en", new Guid("c6000001-0000-0000-0000-00000000000b"), "Week 24-25: Baby is about 30 cm, lungs are developing rapidly. Baby responds to light now!", "Baby is the size of a cantaloupe!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-00000000000b"), "Tuần 24-25: Bé dài khoảng 30 cm, phổi đang phát triển mạnh. Bé phản ứng với ánh sáng rồi mẹ ạ!", "Bé to bằng quả dưa lưới!" },
                    { "en", new Guid("c6000001-0000-0000-0000-00000000000c"), "Week 26-27: Baby is 36.6 cm, eyes can open now. Baby is practicing breathing in the womb!", "Baby is the size of a broccoli!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-00000000000c"), "Tuần 26-27: Bé dài 36.6 cm, mắt bé đã mở được. Bé đang tập thở trong bụng mẹ!", "Bé to bằng bông cải xanh!" },
                    { "en", new Guid("c6000001-0000-0000-0000-00000000000d"), "Week 28-29: Baby is 38.6 cm, weighing about 1 kg. Brain is developing very rapidly now!", "Baby is the size of a butternut squash!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-00000000000d"), "Tuần 28-29: Bé dài 38.6 cm, nặng khoảng 1 kg. Não bé phát triển rất nhanh giai đoạn này!", "Bé to bằng quả bí ngô!" },
                    { "en", new Guid("c6000001-0000-0000-0000-00000000000e"), "Week 30-31: Baby is 40 cm, building up fat to stay warm after birth. Get more rest!", "Baby is the size of a coconut!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-00000000000e"), "Tuần 30-31: Bé dài 40 cm, bé tích mỡ để giữ ấm sau khi sinh. Mẹ nên nghỉ ngơi nhiều hơn!", "Bé to bằng quả dừa!" },
                    { "en", new Guid("c6000001-0000-0000-0000-00000000000f"), "Week 32-33: Baby is 42.4 cm, bones are nearly complete. Start preparing the nursery!", "Baby is the size of a pineapple!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-00000000000f"), "Tuần 32-33: Bé dài 42.4 cm, xương bé gần như hoàn thiện. Mẹ bắt đầu chuẩn bị đồ sơ sinh nhé!", "Bé to bằng quả dứa!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000010"), "Week 34-35: Baby is 45 cm, lungs are nearly mature. Count baby movements daily!", "Baby is the size of a honeydew melon!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000010"), "Tuần 34-35: Bé dài 45 cm, phổi gần trưởng thành. Mẹ nhớ đếm cử động bé hàng ngày!", "Bé to bằng quả dưa!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000011"), "Week 36-37: Baby is 47.4 cm, head has turned down. Almost time to meet your baby!", "Baby is the size of a papaya!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000011"), "Tuần 36-37: Bé dài 47.4 cm, đầu bé đã quay xuống. Sắp được gặp con rồi mẹ ơi!", "Bé to bằng quả bưởi!" },
                    { "en", new Guid("c6000001-0000-0000-0000-000000000012"), "Week 38-40: Baby is about 50 cm, weighing 3-3.5 kg. Baby is full-term and ready to be born!", "Baby is the size of a watermelon!" },
                    { "vi", new Guid("c6000001-0000-0000-0000-000000000012"), "Tuần 38-40: Bé dài khoảng 50 cm, nặng 3-3.5 kg. Bé đủ tháng và sẵn sàng chào đời!", "Bé to bằng quả dưa hấu!" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000001"), "Week 8: Baby's heart beats 120-160 bpm — twice as fast as yours! You can hear it via ultrasound.", "Baby's heart is beating! 💓" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000001"), "Tuần 8: Tim bé đang đập 120-160 nhịp/phút, nhanh gấp đôi mẹ! Mẹ có thể nghe thấy qua siêu âm.", "Tim bé đập rồi! 💓" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000002"), "Week 12: Baby begins swallowing amniotic fluid — it's how they practice eating before birth!", "Baby can swallow! 🍼" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000002"), "Tuần 12: Bé bắt đầu tập nuốt nước ối — đây là cách bé tập ăn trước khi ra đời!", "Bé biết nuốt! 🍼" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000003"), "Week 16: You may start feeling baby's movements — those first kicks are magical!", "Baby can kick! 🦶" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000003"), "Tuần 16: Mẹ bắt đầu cảm nhận bé cử động — những cú đạp đầu tiên thật tuyệt vời!", "Bé biết đạp! 🦶" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000004"), "Week 20: Baby can hear your voice! Sing and talk to your little one regularly.", "Baby can hear you! 👂" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000004"), "Tuần 20: Bé đã nghe được giọng mẹ! Hãy hát và nói chuyện với bé nhiều nhé.", "Bé nghe được rồi! 👂" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000005"), "Week 24: Air sacs are forming in baby's lungs. Baby could survive outside the womb with medical support.", "Baby's lungs are developing! 🫁" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000005"), "Tuần 24: Phổi bé đang hình thành túi khí. Bé có thể sống ngoài tử cung nếu sinh non (với hỗ trợ y tế).", "Phổi bé phát triển! 🫁" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000006"), "Week 28: Baby's eyes are open and can see light filtering through from outside!", "Baby opens eyes! 👀" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000006"), "Tuần 28: Bé đã mở mắt và nhìn thấy ánh sáng từ bên ngoài bụng mẹ!", "Bé mở mắt! 👀" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000007"), "Week 32: Most babies have turned head-down, getting ready for delivery day.", "Baby turns head down! 🔄" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000007"), "Tuần 32: Hầu hết bé đã quay đầu xuống dưới, sẵn sàng cho ngày sinh.", "Bé quay đầu! 🔄" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000008"), "Week 36: Baby is nearly fully developed. Start packing your hospital bag!", "Baby is almost ready! ✨" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000008"), "Tuần 36: Bé đã phát triển gần hoàn thiện. Mẹ nên chuẩn bị túi đồ đi sinh nhé!", "Bé sẵn sàng! ✨" },
                    { "en", new Guid("c6000002-0000-0000-0000-000000000009"), "Week 38-40: Baby is ready to be born! Stay calm and trust yourself.", "Baby is full-term! 🎉" },
                    { "vi", new Guid("c6000002-0000-0000-0000-000000000009"), "Tuần 38-40: Bé đã sẵn sàng chào đời! Mẹ bình tĩnh và tin tưởng vào bản thân nhé.", "Bé đủ tháng! 🎉" },
                    { "en", new Guid("c6000003-0000-0000-0000-000000000001"), "Months 1-3: Take 400mcg folic acid daily, eat small frequent meals to reduce nausea, drink 2L water daily.", "First trimester tips 💊" },
                    { "vi", new Guid("c6000003-0000-0000-0000-000000000001"), "3 tháng đầu: Bổ sung acid folic 400mcg/ngày, ăn ít nhưng nhiều bữa để giảm ốm nghén, uống đủ 2L nước/ngày.", "Mẹo tam cá nguyệt 1 💊" },
                    { "en", new Guid("c6000003-0000-0000-0000-000000000002"), "Months 4-6: Your most energetic period! Light exercise (yoga, walking), take iron & calcium, monitor weight regularly.", "Second trimester tips 🏃‍♀️" },
                    { "vi", new Guid("c6000003-0000-0000-0000-000000000002"), "3 tháng giữa: Giai đoạn mẹ khỏe nhất! Tập thể dục nhẹ (yoga, đi bộ), bổ sung sắt + canxi, theo dõi cân nặng đều đặn.", "Mẹo tam cá nguyệt 2 🏃‍♀️" },
                    { "en", new Guid("c6000003-0000-0000-0000-000000000003"), "Months 7-9: Count baby movements (10+/day), prepare baby essentials, rest well, sleep on your left side for better circulation.", "Third trimester tips 🧸" },
                    { "vi", new Guid("c6000003-0000-0000-0000-000000000003"), "3 tháng cuối: Đếm cử động bé (>10 lần/ngày), chuẩn bị đồ sơ sinh, nghỉ ngơi nhiều, nằm nghiêng trái để tăng tuần hoàn.", "Mẹo tam cá nguyệt 3 🧸" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_motivational_template_translations_language_code",
                table: "motivational_template_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "idx_motivational_week",
                table: "motivational_templates",
                columns: new[] { "week_start", "week_end", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_weight_alerts_pregnancy",
                table: "weight_alerts",
                columns: new[] { "pregnancy_id", "triggered_at" });

            migrationBuilder.CreateIndex(
                name: "idx_weight_alerts_type",
                table: "weight_alerts",
                columns: new[] { "alert_type", "triggered_at" });

            migrationBuilder.CreateIndex(
                name: "uk_weight_goals_pregnancy",
                table: "weight_goal_ranges",
                column: "pregnancy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_weight_logs_pregnancy_date",
                table: "weight_logs",
                columns: new[] { "pregnancy_id", "logged_on" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "motivational_template_translations");

            migrationBuilder.DropTable(
                name: "weight_alerts");

            migrationBuilder.DropTable(
                name: "weight_goal_ranges");

            migrationBuilder.DropTable(
                name: "weight_logs");

            migrationBuilder.DropTable(
                name: "motivational_templates");
        }
    }
}
