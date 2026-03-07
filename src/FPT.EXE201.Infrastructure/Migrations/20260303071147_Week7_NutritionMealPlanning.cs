using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FPT.EXE201.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Week7_NutritionMealPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_request_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    feature = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    template_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prompt_version = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    request_payload = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    response_payload = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tokens_input = table.Column<int>(type: "int", nullable: true),
                    tokens_output = table.Column<int>(type: "int", nullable: true),
                    processing_time_ms = table.Column<int>(type: "int", nullable: true),
                    error_message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_request_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_request_logs_ai_prompt_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "ai_prompt_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ai_request_logs_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ai_request_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pregnancy_nutrition_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    note_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value_text = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pregnancy_nutrition_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_pregnancy_nutrition_notes_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    instructions = table.Column<string>(type: "LONGTEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    servings = table.Column<int>(type: "int", nullable: true),
                    prep_minutes = table.Column<int>(type: "int", nullable: true),
                    cook_minutes = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipes", x => x.id);
                    table.ForeignKey(
                        name: "FK_recipes_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_food_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_food_items", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_nutrients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_nutrients", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    ai_request_log_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    start_date = table.Column<DateOnly>(type: "DATE", nullable: false),
                    end_date = table.Column<DateOnly>(type: "DATE", nullable: false),
                    source = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_plans", x => x.id);
                    table.CheckConstraint("chk_meal_plan_dates", "end_date >= start_date");
                    table.ForeignKey(
                        name: "FK_meal_plans_ai_request_logs_ai_request_log_id",
                        column: x => x.ai_request_log_id,
                        principalTable: "ai_request_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_meal_plans_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pregnancy_food_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    pregnancy_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    food_item_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    preference_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pregnancy_food_preferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_pregnancy_food_preferences_pregnancies_pregnancy_id",
                        column: x => x.pregnancy_id,
                        principalTable: "pregnancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pregnancy_food_preferences_ref_food_items_food_item_id",
                        column: x => x.food_item_id,
                        principalTable: "ref_food_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_food_item_translations",
                columns: table => new
                {
                    food_item_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    language_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_food_item_translations", x => new { x.food_item_id, x.language_code });
                    table.ForeignKey(
                        name: "FK_ref_food_item_translations_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ref_food_item_translations_ref_food_items_food_item_id",
                        column: x => x.food_item_id,
                        principalTable: "ref_food_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_nutrient_translations",
                columns: table => new
                {
                    nutrient_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    language_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_nutrient_translations", x => new { x.nutrient_id, x.language_code });
                    table.ForeignKey(
                        name: "FK_ref_nutrient_translations_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ref_nutrient_translations_ref_nutrients_nutrient_id",
                        column: x => x.nutrient_id,
                        principalTable: "ref_nutrients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_plan_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    meal_plan_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_date = table.Column<DateOnly>(type: "DATE", nullable: false),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_plan_days", x => x.id);
                    table.ForeignKey(
                        name: "FK_meal_plan_days_meal_plans_meal_plan_id",
                        column: x => x.meal_plan_id,
                        principalTable: "meal_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_plan_feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    meal_plan_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    rating = table.Column<sbyte>(type: "TINYINT", nullable: false),
                    comment = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_plan_feedback", x => x.id);
                    table.CheckConstraint("chk_plan_rating", "rating BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_meal_plan_feedback_meal_plans_meal_plan_id",
                        column: x => x.meal_plan_id,
                        principalTable: "meal_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_plan_feedback_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    meal_day_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    meal_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipe_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    item_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    portion_text = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    calories_kcal = table.Column<int>(type: "int", nullable: true),
                    notes = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_items", x => x.id);
                    table.CheckConstraint("chk_meal_item_name", "recipe_id IS NOT NULL OR item_name IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_meal_items_meal_plan_days_meal_day_id",
                        column: x => x.meal_day_id,
                        principalTable: "meal_plan_days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_items_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_item_feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    meal_item_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    liked = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    comment = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "DATETIME(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_item_feedback", x => x.id);
                    table.ForeignKey(
                        name: "FK_meal_item_feedback_meal_items_meal_item_id",
                        column: x => x.meal_item_id,
                        principalTable: "meal_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_item_feedback_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_item_nutrients",
                columns: table => new
                {
                    meal_item_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    nutrient_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    amount = table.Column<decimal>(type: "DECIMAL(10,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_item_nutrients", x => new { x.meal_item_id, x.nutrient_id });
                    table.CheckConstraint("chk_nutrient_amount", "amount >= 0");
                    table.ForeignKey(
                        name: "FK_meal_item_nutrients_meal_items_meal_item_id",
                        column: x => x.meal_item_id,
                        principalTable: "meal_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_item_nutrients_ref_nutrients_nutrient_id",
                        column: x => x.nutrient_id,
                        principalTable: "ref_nutrients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ai_prompt_templates",
                columns: new[] { "id", "created_at", "deleted_at", "description", "display_name", "domain_rules", "feature_rules", "is_active", "max_output_tokens", "model_name", "output_schema", "system_rules", "temperature", "template_key", "updated_at", "version" },
                values: new object[] { new Guid("a1000002-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Generate 7-day AI meal plans with Vietnamese dishes, recipes, and nutrients for pregnant women.", "Nutrition Meal Plan Generator", "Pregnancy nutrition guidelines (IOM):\r\n- Trimester 1 (week 1-12): Focus folic acid (600mcg/day), no extra calories.\r\n- Trimester 2 (week 13-26): +340 kcal/day, iron (27mg/day), calcium (1000mg/day).\r\n- Trimester 3 (week 27-40): +450 kcal/day, increase protein, DHA.\r\n- Daily water: 2.3L minimum.\r\n- Avoid: raw fish, high-mercury fish, unpasteurized dairy, alcohol.\r\n- Gestational diabetes: low GI foods, split meals, limit sugar.\r\n- Preeclampsia: reduce sodium, increase potassium.", "Generate a 7-day meal plan with exactly 4 meals per day: BREAKFAST, LUNCH, DINNER, SNACK.\r\n\r\nFor EVERY meal item, you MUST provide:\r\n- itemName: Vietnamese dish name (concise)\r\n- portionText: serving size in Vietnamese\r\n- caloriesKcal: integer\r\n- notes: brief nutrition note in Vietnamese (nullable)\r\n- recipe: REQUIRED object with:\r\n  - title: dish name\r\n  - instructions: step-by-step cooking instructions in Vietnamese\r\n  - servings: integer\r\n  - prepMinutes: integer\r\n  - cookMinutes: integer\r\n- nutrients: array of objects, ONLY use these codes:\r\n  PROTEIN, CARBOHYDRATES, FAT, FIBER, IRON, CALCIUM,\r\n  FOLIC_ACID, VITAMIN_D, VITAMIN_C, VITAMIN_A,\r\n  VITAMIN_B12, OMEGA_3, DHA, ZINC\r\n  Each: { \"code\": \"PROTEIN\", \"amount\": 12.5 }\r\n\r\nEnsure variety: do not repeat the same dish within 3 days.\r\nEach day's total calories should be close to {targetCalories} kcal.", true, 8192, "gemini-2.5-flash", "{\r\n  \"title\": \"string\",\r\n  \"totalDailyCalories\": \"number\",\r\n  \"notes\": \"string\",\r\n  \"days\": [\r\n    {\r\n      \"date\": \"YYYY-MM-DD\",\r\n      \"meals\": [\r\n        {\r\n          \"mealType\": \"BREAKFAST|LUNCH|DINNER|SNACK\",\r\n          \"itemName\": \"string\",\r\n          \"portionText\": \"string\",\r\n          \"caloriesKcal\": \"number\",\r\n          \"notes\": \"string|null\",\r\n          \"recipe\": {\r\n            \"title\": \"string\",\r\n            \"instructions\": \"string\",\r\n            \"servings\": \"number\",\r\n            \"prepMinutes\": \"number\",\r\n            \"cookMinutes\": \"number\"\r\n          },\r\n          \"nutrients\": [\r\n            { \"code\": \"string\", \"amount\": \"number\" }\r\n          ]\r\n        }\r\n      ]\r\n    }\r\n  ]\r\n}", "You are a certified prenatal nutritionist AI assistant.\r\nRespond in Vietnamese.\r\nOutput ONLY valid JSON matching the provided schema.\r\nNo markdown, no explanation, no extra text outside JSON.", 0.7m, "nutrition.meal_plan", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.InsertData(
                table: "ref_food_items",
                columns: new[] { "id", "code", "created_at", "deleted_at", "is_active", "updated_at" },
                values: new object[,]
                {
                    { new Guid("c7010001-0000-0000-0000-000000000001"), "CHICKEN", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-000000000002"), "PORK", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-000000000003"), "BEEF", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-000000000004"), "FISH_SALMON", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-000000000005"), "FISH_TUNA", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-000000000006"), "FISH_MACKEREL", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-000000000007"), "SHRIMP", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-000000000008"), "CRAB", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-000000000009"), "SQUID", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-00000000000a"), "CLAM", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-00000000000b"), "EGG", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-00000000000c"), "TOFU", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010001-0000-0000-0000-00000000000d"), "TEMPEH", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010002-0000-0000-0000-000000000001"), "SEAFOOD_GENERAL", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010002-0000-0000-0000-000000000002"), "PEANUT", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010002-0000-0000-0000-000000000003"), "TREE_NUT", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010002-0000-0000-0000-000000000004"), "MILK_COW", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010002-0000-0000-0000-000000000005"), "GLUTEN", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010002-0000-0000-0000-000000000006"), "SOYBEAN", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010002-0000-0000-0000-000000000007"), "SHELLFISH", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010002-0000-0000-0000-000000000008"), "SESAME", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000001"), "CILANTRO", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000002"), "BITTER_MELON", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000003"), "MORNING_GLORY", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000004"), "SPINACH", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000005"), "BOK_CHOY", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000006"), "BEAN_SPROUT", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000007"), "ONION", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000008"), "GARLIC", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010003-0000-0000-0000-000000000009"), "GINGER", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010004-0000-0000-0000-000000000001"), "DURIAN", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010004-0000-0000-0000-000000000002"), "JACKFRUIT", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010004-0000-0000-0000-000000000003"), "PINEAPPLE", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010004-0000-0000-0000-000000000004"), "PAPAYA_GREEN", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010005-0000-0000-0000-000000000001"), "SHRIMP_PASTE", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010005-0000-0000-0000-000000000002"), "FISH_SAUCE_STRONG", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010005-0000-0000-0000-000000000003"), "MSG", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010005-0000-0000-0000-000000000004"), "ORGAN_MEAT_LIVER", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010005-0000-0000-0000-000000000005"), "ORGAN_MEAT_GENERAL", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010005-0000-0000-0000-000000000006"), "CAFFEINE", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010005-0000-0000-0000-000000000007"), "ALCOHOL", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010006-0000-0000-0000-000000000001"), "RAW_FISH", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010006-0000-0000-0000-000000000002"), "SOFT_CHEESE", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010006-0000-0000-0000-000000000003"), "RAW_EGG", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7010006-0000-0000-0000-000000000004"), "DELI_MEAT", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ref_nutrients",
                columns: new[] { "id", "code", "created_at", "is_active", "unit", "updated_at" },
                values: new object[,]
                {
                    { new Guid("c7020001-0000-0000-0000-000000000001"), "CALORIES", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "kcal", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-000000000002"), "PROTEIN", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "g", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-000000000003"), "CARBOHYDRATES", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "g", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-000000000004"), "FAT", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "g", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-000000000005"), "FIBER", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "g", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-000000000006"), "IRON", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-000000000007"), "CALCIUM", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-000000000008"), "FOLIC_ACID", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mcg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-000000000009"), "VITAMIN_D", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mcg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-00000000000a"), "VITAMIN_C", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-00000000000b"), "VITAMIN_A", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mcg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-00000000000c"), "VITAMIN_B12", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mcg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-00000000000d"), "OMEGA_3", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-00000000000e"), "DHA", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7020001-0000-0000-0000-00000000000f"), "ZINC", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "mg", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ref_food_item_translations",
                columns: new[] { "food_item_id", "language_code", "display_name" },
                values: new object[,]
                {
                    { new Guid("c7010001-0000-0000-0000-000000000001"), "en", "Chicken" },
                    { new Guid("c7010001-0000-0000-0000-000000000001"), "vi", "Thịt gà" },
                    { new Guid("c7010001-0000-0000-0000-000000000002"), "en", "Pork" },
                    { new Guid("c7010001-0000-0000-0000-000000000002"), "vi", "Thịt heo" },
                    { new Guid("c7010001-0000-0000-0000-000000000003"), "en", "Beef" },
                    { new Guid("c7010001-0000-0000-0000-000000000003"), "vi", "Thịt bò" },
                    { new Guid("c7010001-0000-0000-0000-000000000004"), "en", "Salmon" },
                    { new Guid("c7010001-0000-0000-0000-000000000004"), "vi", "Cá hồi" },
                    { new Guid("c7010001-0000-0000-0000-000000000005"), "en", "Tuna" },
                    { new Guid("c7010001-0000-0000-0000-000000000005"), "vi", "Cá ngừ" },
                    { new Guid("c7010001-0000-0000-0000-000000000006"), "en", "Mackerel" },
                    { new Guid("c7010001-0000-0000-0000-000000000006"), "vi", "Cá thu" },
                    { new Guid("c7010001-0000-0000-0000-000000000007"), "en", "Shrimp" },
                    { new Guid("c7010001-0000-0000-0000-000000000007"), "vi", "Tôm" },
                    { new Guid("c7010001-0000-0000-0000-000000000008"), "en", "Crab" },
                    { new Guid("c7010001-0000-0000-0000-000000000008"), "vi", "Cua" },
                    { new Guid("c7010001-0000-0000-0000-000000000009"), "en", "Squid" },
                    { new Guid("c7010001-0000-0000-0000-000000000009"), "vi", "Mực" },
                    { new Guid("c7010001-0000-0000-0000-00000000000a"), "en", "Clam / Mussel" },
                    { new Guid("c7010001-0000-0000-0000-00000000000a"), "vi", "Nghêu / Sò" },
                    { new Guid("c7010001-0000-0000-0000-00000000000b"), "en", "Egg" },
                    { new Guid("c7010001-0000-0000-0000-00000000000b"), "vi", "Trứng" },
                    { new Guid("c7010001-0000-0000-0000-00000000000c"), "en", "Tofu" },
                    { new Guid("c7010001-0000-0000-0000-00000000000c"), "vi", "Đậu phụ" },
                    { new Guid("c7010001-0000-0000-0000-00000000000d"), "en", "Tempeh" },
                    { new Guid("c7010001-0000-0000-0000-00000000000d"), "vi", "Tempeh" },
                    { new Guid("c7010002-0000-0000-0000-000000000001"), "en", "Seafood (general)" },
                    { new Guid("c7010002-0000-0000-0000-000000000001"), "vi", "Hải sản (chung)" },
                    { new Guid("c7010002-0000-0000-0000-000000000002"), "en", "Peanut" },
                    { new Guid("c7010002-0000-0000-0000-000000000002"), "vi", "Đậu phộng" },
                    { new Guid("c7010002-0000-0000-0000-000000000003"), "en", "Tree nuts (walnut, almond...)" },
                    { new Guid("c7010002-0000-0000-0000-000000000003"), "vi", "Hạt cây (óc chó, hạnh nhân...)" },
                    { new Guid("c7010002-0000-0000-0000-000000000004"), "en", "Cow's milk" },
                    { new Guid("c7010002-0000-0000-0000-000000000004"), "vi", "Sữa bò" },
                    { new Guid("c7010002-0000-0000-0000-000000000005"), "en", "Gluten (wheat)" },
                    { new Guid("c7010002-0000-0000-0000-000000000005"), "vi", "Gluten (lúa mì)" },
                    { new Guid("c7010002-0000-0000-0000-000000000006"), "en", "Soybean" },
                    { new Guid("c7010002-0000-0000-0000-000000000006"), "vi", "Đậu nành" },
                    { new Guid("c7010002-0000-0000-0000-000000000007"), "en", "Shellfish" },
                    { new Guid("c7010002-0000-0000-0000-000000000007"), "vi", "Động vật có vỏ" },
                    { new Guid("c7010002-0000-0000-0000-000000000008"), "en", "Sesame" },
                    { new Guid("c7010002-0000-0000-0000-000000000008"), "vi", "Mè (vừng)" },
                    { new Guid("c7010003-0000-0000-0000-000000000001"), "en", "Cilantro (coriander)" },
                    { new Guid("c7010003-0000-0000-0000-000000000001"), "vi", "Rau mùi (ngò)" },
                    { new Guid("c7010003-0000-0000-0000-000000000002"), "en", "Bitter melon" },
                    { new Guid("c7010003-0000-0000-0000-000000000002"), "vi", "Khổ qua (mướp đắng)" },
                    { new Guid("c7010003-0000-0000-0000-000000000003"), "en", "Morning glory (water spinach)" },
                    { new Guid("c7010003-0000-0000-0000-000000000003"), "vi", "Rau muống" },
                    { new Guid("c7010003-0000-0000-0000-000000000004"), "en", "Spinach" },
                    { new Guid("c7010003-0000-0000-0000-000000000004"), "vi", "Rau bina (cải bó xôi)" },
                    { new Guid("c7010003-0000-0000-0000-000000000005"), "en", "Bok choy" },
                    { new Guid("c7010003-0000-0000-0000-000000000005"), "vi", "Cải thìa" },
                    { new Guid("c7010003-0000-0000-0000-000000000006"), "en", "Bean sprouts" },
                    { new Guid("c7010003-0000-0000-0000-000000000006"), "vi", "Giá đỗ" },
                    { new Guid("c7010003-0000-0000-0000-000000000007"), "en", "Onion" },
                    { new Guid("c7010003-0000-0000-0000-000000000007"), "vi", "Hành" },
                    { new Guid("c7010003-0000-0000-0000-000000000008"), "en", "Garlic" },
                    { new Guid("c7010003-0000-0000-0000-000000000008"), "vi", "Tỏi" },
                    { new Guid("c7010003-0000-0000-0000-000000000009"), "en", "Ginger" },
                    { new Guid("c7010003-0000-0000-0000-000000000009"), "vi", "Gừng" },
                    { new Guid("c7010004-0000-0000-0000-000000000001"), "en", "Durian" },
                    { new Guid("c7010004-0000-0000-0000-000000000001"), "vi", "Sầu riêng" },
                    { new Guid("c7010004-0000-0000-0000-000000000002"), "en", "Jackfruit" },
                    { new Guid("c7010004-0000-0000-0000-000000000002"), "vi", "Mít" },
                    { new Guid("c7010004-0000-0000-0000-000000000003"), "en", "Pineapple" },
                    { new Guid("c7010004-0000-0000-0000-000000000003"), "vi", "Dứa (thơm)" },
                    { new Guid("c7010004-0000-0000-0000-000000000004"), "en", "Green papaya" },
                    { new Guid("c7010004-0000-0000-0000-000000000004"), "vi", "Đu đủ xanh" },
                    { new Guid("c7010005-0000-0000-0000-000000000001"), "en", "Shrimp paste" },
                    { new Guid("c7010005-0000-0000-0000-000000000001"), "vi", "Mắm tôm" },
                    { new Guid("c7010005-0000-0000-0000-000000000002"), "en", "Strong fish sauce" },
                    { new Guid("c7010005-0000-0000-0000-000000000002"), "vi", "Nước mắm nặng mùi" },
                    { new Guid("c7010005-0000-0000-0000-000000000003"), "en", "MSG" },
                    { new Guid("c7010005-0000-0000-0000-000000000003"), "vi", "Bột ngọt (MSG)" },
                    { new Guid("c7010005-0000-0000-0000-000000000004"), "en", "Liver" },
                    { new Guid("c7010005-0000-0000-0000-000000000004"), "vi", "Gan" },
                    { new Guid("c7010005-0000-0000-0000-000000000005"), "en", "Organ meat (general)" },
                    { new Guid("c7010005-0000-0000-0000-000000000005"), "vi", "Nội tạng (chung)" },
                    { new Guid("c7010005-0000-0000-0000-000000000006"), "en", "Caffeine" },
                    { new Guid("c7010005-0000-0000-0000-000000000006"), "vi", "Caffeine" },
                    { new Guid("c7010005-0000-0000-0000-000000000007"), "en", "Alcohol" },
                    { new Guid("c7010005-0000-0000-0000-000000000007"), "vi", "Rượu bia" },
                    { new Guid("c7010006-0000-0000-0000-000000000001"), "en", "Raw fish / Sashimi" },
                    { new Guid("c7010006-0000-0000-0000-000000000001"), "vi", "Cá sống / Sashimi" },
                    { new Guid("c7010006-0000-0000-0000-000000000002"), "en", "Soft cheese" },
                    { new Guid("c7010006-0000-0000-0000-000000000002"), "vi", "Phô mai mềm" },
                    { new Guid("c7010006-0000-0000-0000-000000000003"), "en", "Raw egg" },
                    { new Guid("c7010006-0000-0000-0000-000000000003"), "vi", "Trứng sống" },
                    { new Guid("c7010006-0000-0000-0000-000000000004"), "en", "Deli meat" },
                    { new Guid("c7010006-0000-0000-0000-000000000004"), "vi", "Thịt nguội (deli meat)" }
                });

            migrationBuilder.InsertData(
                table: "ref_nutrient_translations",
                columns: new[] { "language_code", "nutrient_id", "display_name" },
                values: new object[,]
                {
                    { "en", new Guid("c7020001-0000-0000-0000-000000000001"), "Calories" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000001"), "Năng lượng" },
                    { "en", new Guid("c7020001-0000-0000-0000-000000000002"), "Protein" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000002"), "Chất đạm" },
                    { "en", new Guid("c7020001-0000-0000-0000-000000000003"), "Carbohydrates" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000003"), "Tinh bột" },
                    { "en", new Guid("c7020001-0000-0000-0000-000000000004"), "Fat" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000004"), "Chất béo" },
                    { "en", new Guid("c7020001-0000-0000-0000-000000000005"), "Fiber" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000005"), "Chất xơ" },
                    { "en", new Guid("c7020001-0000-0000-0000-000000000006"), "Iron" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000006"), "Sắt" },
                    { "en", new Guid("c7020001-0000-0000-0000-000000000007"), "Calcium" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000007"), "Canxi" },
                    { "en", new Guid("c7020001-0000-0000-0000-000000000008"), "Folic acid" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000008"), "Axit folic" },
                    { "en", new Guid("c7020001-0000-0000-0000-000000000009"), "Vitamin D" },
                    { "vi", new Guid("c7020001-0000-0000-0000-000000000009"), "Vitamin D" },
                    { "en", new Guid("c7020001-0000-0000-0000-00000000000a"), "Vitamin C" },
                    { "vi", new Guid("c7020001-0000-0000-0000-00000000000a"), "Vitamin C" },
                    { "en", new Guid("c7020001-0000-0000-0000-00000000000b"), "Vitamin A" },
                    { "vi", new Guid("c7020001-0000-0000-0000-00000000000b"), "Vitamin A" },
                    { "en", new Guid("c7020001-0000-0000-0000-00000000000c"), "Vitamin B12" },
                    { "vi", new Guid("c7020001-0000-0000-0000-00000000000c"), "Vitamin B12" },
                    { "en", new Guid("c7020001-0000-0000-0000-00000000000d"), "Omega-3" },
                    { "vi", new Guid("c7020001-0000-0000-0000-00000000000d"), "Omega-3" },
                    { "en", new Guid("c7020001-0000-0000-0000-00000000000e"), "DHA" },
                    { "vi", new Guid("c7020001-0000-0000-0000-00000000000e"), "DHA" },
                    { "en", new Guid("c7020001-0000-0000-0000-00000000000f"), "Zinc" },
                    { "vi", new Guid("c7020001-0000-0000-0000-00000000000f"), "Kẽm" }
                });

            migrationBuilder.CreateIndex(
                name: "idx_ai_logs_feature",
                table: "ai_request_logs",
                columns: new[] { "feature", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_ai_logs_pregnancy",
                table: "ai_request_logs",
                columns: new[] { "pregnancy_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_ai_logs_status",
                table: "ai_request_logs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_request_logs_template_id",
                table: "ai_request_logs",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_request_logs_user_id",
                table: "ai_request_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_meal_item_feedback_user_id",
                table: "meal_item_feedback",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uk_meal_item_feedback",
                table: "meal_item_feedback",
                columns: new[] { "meal_item_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meal_item_nutrients_nutrient_id",
                table: "meal_item_nutrients",
                column: "nutrient_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_items_day_type",
                table: "meal_items",
                columns: new[] { "meal_day_id", "meal_type" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_items_recipe_id",
                table: "meal_items",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "uk_meal_plan_days",
                table: "meal_plan_days",
                columns: new[] { "meal_plan_id", "plan_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meal_plan_feedback_user_id",
                table: "meal_plan_feedback",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uk_meal_plan_feedback",
                table: "meal_plan_feedback",
                columns: new[] { "meal_plan_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_meal_plans_pregnancy",
                table: "meal_plans",
                columns: new[] { "pregnancy_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_plans_ai_request_log_id",
                table: "meal_plans",
                column: "ai_request_log_id");

            migrationBuilder.CreateIndex(
                name: "IX_pregnancy_food_preferences_food_item_id",
                table: "pregnancy_food_preferences",
                column: "food_item_id");

            migrationBuilder.CreateIndex(
                name: "uk_food_pref_pregnancy",
                table: "pregnancy_food_preferences",
                columns: new[] { "pregnancy_id", "food_item_id", "preference_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_nutrition_notes_pregnancy",
                table: "pregnancy_nutrition_notes",
                columns: new[] { "pregnancy_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_recipes_pregnancy",
                table: "recipes",
                columns: new[] { "pregnancy_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ref_food_item_translations_language_code",
                table: "ref_food_item_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "uk_ref_food_items_code",
                table: "ref_food_items",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ref_nutrient_translations_language_code",
                table: "ref_nutrient_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "uk_ref_nutrients_code",
                table: "ref_nutrients",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meal_item_feedback");

            migrationBuilder.DropTable(
                name: "meal_item_nutrients");

            migrationBuilder.DropTable(
                name: "meal_plan_feedback");

            migrationBuilder.DropTable(
                name: "pregnancy_food_preferences");

            migrationBuilder.DropTable(
                name: "pregnancy_nutrition_notes");

            migrationBuilder.DropTable(
                name: "ref_food_item_translations");

            migrationBuilder.DropTable(
                name: "ref_nutrient_translations");

            migrationBuilder.DropTable(
                name: "meal_items");

            migrationBuilder.DropTable(
                name: "ref_food_items");

            migrationBuilder.DropTable(
                name: "ref_nutrients");

            migrationBuilder.DropTable(
                name: "meal_plan_days");

            migrationBuilder.DropTable(
                name: "recipes");

            migrationBuilder.DropTable(
                name: "meal_plans");

            migrationBuilder.DropTable(
                name: "ai_request_logs");

            migrationBuilder.DeleteData(
                table: "ai_prompt_templates",
                keyColumn: "id",
                keyValue: new Guid("a1000002-0000-0000-0000-000000000001"));
        }
    }
}
