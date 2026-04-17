using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Seed Roles & Permissions
            await SeedRolesAndPermissionsAsync(context);
        }

        private static async Task SeedRolesAndPermissionsAsync(AppDbContext context)
        {
            // ===========================
            // 1. Seed Roles (Idempotent - Check and Insert)
            // ===========================
            
            // ADMIN Role
            if (!await context.Roles.AnyAsync(r => r.Code == "ADMIN"))
            {
                await context.Roles.AddAsync(new Role
                {
                    Id = Guid.NewGuid(),
                    Code = "ADMIN",
                    Name = "Administrator",
                    Description = "System administrator with full access"
                });
            }

            // USER Role
            if (!await context.Roles.AnyAsync(r => r.Code == "USER"))
            {
                await context.Roles.AddAsync(new Role
                {
                    Id = Guid.NewGuid(),
                    Code = "USER",
                    Name = "User",
                    Description = "Regular user (pregnant mother)"
                });
            }

            // DOCTOR Role
            if (!await context.Roles.AnyAsync(r => r.Code == "DOCTOR"))
            {
                await context.Roles.AddAsync(new Role
                {
                    Id = Guid.NewGuid(),
                    Code = "DOCTOR",
                    Name = "Doctor",
                    Description = "Medical professional with cross-user access"
                });
            }

            // PREMIUM Role
            if (!await context.Roles.AnyAsync(r => r.Code == "PREMIUM"))
            {
                await context.Roles.AddAsync(new Role
                {
                    Id = Guid.NewGuid(),
                    Code = "PREMIUM",
                    Name = "Premium User",
                    Description = "User with active premium subscription"
                });
            }

            await context.SaveChangesAsync();

            // Load roles from database for permission assignment
            var adminRole = await context.Roles.FirstAsync(r => r.Code == "ADMIN");
            var userRole = await context.Roles.FirstAsync(r => r.Code == "USER");
            var doctorRole = await context.Roles.FirstAsync(r => r.Code == "DOCTOR");
            var premiumRole = await context.Roles.FirstAsync(r => r.Code == "PREMIUM");

            // ===========================
            // 2. Seed Permissions (Idempotent - Check and Insert)
            // ===========================
            await SeedPermissionIfNotExists(context, "user_profiles.write.own", "Update Own User Profile", "User can update their own profile");
            await SeedPermissionIfNotExists(context, "user_profiles.read.any", "Read Any User Profile", "Admin can view any user profile");
            await SeedPermissionIfNotExists(context, "user_profiles.write.any", "Update Any User Profile", "Admin can update any user profile");

            await SeedPermissionIfNotExists(context, "users.read.any", "Read Any User", "Admin can view all users");
            await SeedPermissionIfNotExists(context, "users.update.any", "Update Any User", "Admin can update any user");
            await SeedPermissionIfNotExists(context, "users.delete.any", "Delete Any User", "Admin can delete users");
            await SeedPermissionIfNotExists(context, "users.impersonate", "Impersonate User", "Admin can login as another user for debugging");

            await SeedPermissionIfNotExists(context, "rbac.roles.read", "Read Roles", "View roles list");
            await SeedPermissionIfNotExists(context, "rbac.roles.write", "Manage Roles", "Create/update roles");
            await SeedPermissionIfNotExists(context, "rbac.permissions.read", "Read Permissions", "View permissions list");
            await SeedPermissionIfNotExists(context, "rbac.user_roles.assign", "Assign User Roles", "Assign roles to users");
            await SeedPermissionIfNotExists(context, "rbac.user_roles.remove", "Remove User Roles", "Remove roles from users");

            await SeedPermissionIfNotExists(context, "audit.read", "Read Audit Logs", "View audit event logs");
            await SeedPermissionIfNotExists(context, "audit.export", "Export Audit Logs", "Export audit logs to file");
            await SeedPermissionIfNotExists(context, "system.read", "Read System Settings", "View system configuration");
            await SeedPermissionIfNotExists(context, "system.write", "Update System Settings", "Modify system configuration");

            await SeedPermissionIfNotExists(context, "pregnancies.write.own", "Manage Own Pregnancy", "User can create/update their own pregnancy data");
            await SeedPermissionIfNotExists(context, "pregnancies.read.any", "Read Any Pregnancy", "Doctor/Admin can view any pregnancy data");
            await SeedPermissionIfNotExists(context, "pregnancies.update.any", "Update Any Pregnancy", "Doctor can update pregnancy medical info");
            await SeedPermissionIfNotExists(context, "pregnancies.delete.any", "Delete Any Pregnancy", "Admin can delete pregnancy records");
            await SeedPermissionIfNotExists(context, "pregnancy_conditions.write.any", "Write Pregnancy Conditions", "Doctor can record pregnancy conditions");
            await SeedPermissionIfNotExists(context, "prenatal_visits.write.any", "Write Prenatal Visits", "Doctor can create visit records");
            await SeedPermissionIfNotExists(context, "prenatal_tests.write.any", "Write Prenatal Tests", "Doctor/Lab can record test results");

            await SeedPermissionIfNotExists(context, "documents.read.any", "Read Any Medical Document", "Doctor can view patient documents");
            await SeedPermissionIfNotExists(context, "documents.moderate", "Moderate Documents", "Admin can review/remove inappropriate documents");
            await SeedPermissionIfNotExists(context, "storage.manage", "Manage Storage", "Admin can manage storage files");
            await SeedPermissionIfNotExists(context, "storage.cleanup", "Cleanup Storage", "Admin can delete orphaned files");

            // Week 4 - Medical Document permissions (used by controllers)
            await SeedPermissionIfNotExists(context, "document.create", "Create Document", "User can upload/create medical documents");
            await SeedPermissionIfNotExists(context, "document.view", "View Documents", "User can view their own medical documents");
            await SeedPermissionIfNotExists(context, "document.update", "Update Document", "User can update document metadata");
            await SeedPermissionIfNotExists(context, "document.delete", "Delete Document", "User can soft-delete their own documents");
            await SeedPermissionIfNotExists(context, "document.favorite", "Favorite Document", "User can toggle document favorite status");
            await SeedPermissionIfNotExists(context, "ocr.trigger", "Trigger OCR", "User can trigger OCR rerun for their documents");
            await SeedPermissionIfNotExists(context, "ocr.view", "View OCR Status", "User can check OCR processing status");
            await SeedPermissionIfNotExists(context, "ai.admin", "AI Admin", "Admin can manage AI prompt templates");

            // Week 5.5 - Auto-Fill permissions
            await SeedPermissionIfNotExists(context, "ocr.review", "Review OCR Extraction", "User can review AI-extracted data before confirming");
            await SeedPermissionIfNotExists(context, "ocr.confirm", "Confirm OCR Extraction", "User can confirm extraction and auto-create entities");

            await SeedPermissionIfNotExists(context, "weight_logs.write.own", "Log Own Weight", "User can log their weight");
            await SeedPermissionIfNotExists(context, "weight_logs.read.any", "Read Any Weight Logs", "Doctor can view patient weight logs");
            await SeedPermissionIfNotExists(context, "weight_alerts.manage", "Manage Weight Alerts", "Admin can configure alert rules");
            await SeedPermissionIfNotExists(context, "motivational_templates.write", "Manage Motivational Templates", "Admin can create/update motivational messages");

            // Week 6 — Weight Tracking (granular permissions)
            await SeedPermissionIfNotExists(context, "weight_log.read", "Read Weight Logs", "User can view their own weight logs");
            await SeedPermissionIfNotExists(context, "weight_log.write", "Write Weight Logs", "User can create/update weight logs + OCR extract");
            await SeedPermissionIfNotExists(context, "weight_log.delete", "Delete Weight Logs", "User can delete their own weight logs");
            await SeedPermissionIfNotExists(context, "weight_goal.read", "Read Weight Goals", "User can view their weight goals");
            await SeedPermissionIfNotExists(context, "weight_goal.write", "Write Weight Goals", "User can set/update weight goals");
            await SeedPermissionIfNotExists(context, "weight_alert.read", "Read Weight Alerts", "User can view their weight alerts");
            await SeedPermissionIfNotExists(context, "weight_alert.resolve", "Resolve Weight Alerts", "User can resolve weight alerts");

            await SeedPermissionIfNotExists(context, "meal_plans.write.own", "Manage Own Meal Plans", "User can create/update their meal plans");
            await SeedPermissionIfNotExists(context, "meal_plans.read.any", "Read Any Meal Plan", "Doctor can view patient meal plans");
            await SeedPermissionIfNotExists(context, "nutrition_ai.manage", "Manage Nutrition AI", "Admin can manage AI requests and costs");
            await SeedPermissionIfNotExists(context, "recipes.templates.write", "Manage Recipe Templates", "Admin can create recipe templates");

            await SeedPermissionIfNotExists(context, "doctor_profiles.read", "Read Doctor Profiles", "Public: view doctor directory");
            await SeedPermissionIfNotExists(context, "doctor_profiles.write.own", "Update Own Doctor Profile", "Doctor can update their own profile");
            await SeedPermissionIfNotExists(context, "doctor_profiles.write.any", "Manage Any Doctor Profile", "Admin can manage doctor profiles");
            await SeedPermissionIfNotExists(context, "doctor_profiles.approve", "Approve Doctor Registration", "Admin can approve doctor registrations");

            await SeedPermissionIfNotExists(context, "availability.write.own", "Manage Own Availability", "Doctor can manage their schedule");
            await SeedPermissionIfNotExists(context, "availability.write.any", "Manage Any Availability", "Admin can modify doctor schedules");

            await SeedPermissionIfNotExists(context, "consults.request", "Request Consult", "User can request consult with doctor");
            await SeedPermissionIfNotExists(context, "consults.assign", "Assign Consults", "Admin/System can assign doctor to consults");
            await SeedPermissionIfNotExists(context, "consults.accept", "Accept Consults", "Doctor can accept consult requests");
            await SeedPermissionIfNotExists(context, "consults.view.assigned", "View Assigned Consults", "Doctor can view consults assigned to them");
            await SeedPermissionIfNotExists(context, "consults.view.any", "View Any Consult", "Admin can view all consults");
            await SeedPermissionIfNotExists(context, "consults.cancel.any", "Cancel Any Consult", "Admin can cancel consults");

            await SeedPermissionIfNotExists(context, "chat.send", "Send Chat Messages", "User can send messages in their consult chats");
            await SeedPermissionIfNotExists(context, "calls.join", "Join Video Calls", "User can join video calls with doctors");
            await SeedPermissionIfNotExists(context, "chat.moderate", "Moderate Chat", "Admin can view/remove inappropriate messages");
            await SeedPermissionIfNotExists(context, "chat.participants.manage", "Manage Chat Participants", "Admin can add/remove chat participants");
            await SeedPermissionIfNotExists(context, "chat.export", "Export Chat History", "Export chat history for legal purposes");
            await SeedPermissionIfNotExists(context, "calls.manage", "Manage Calls", "Admin can manage call sessions");
            await SeedPermissionIfNotExists(context, "calls.recordings.access", "Access Call Recordings", "Admin/Doctor can access call recordings");

            await SeedPermissionIfNotExists(context, "reminders.write.own", "Manage Own Reminders", "User can create/update their reminders");
            await SeedPermissionIfNotExists(context, "reminders.templates.write", "Manage Reminder Templates", "Admin can create reminder templates");
            await SeedPermissionIfNotExists(context, "reminders.manage.any", "Manage Any Reminder", "Admin can manage user reminders");

            await SeedPermissionIfNotExists(context, "medical_fields.write", "Manage Medical Field Definitions", "Admin can manage field dictionary");
            await SeedPermissionIfNotExists(context, "medical_data.export", "Export Medical Data", "Doctor can export structured medical data");

            await SeedPermissionIfNotExists(context, "premium.access", "Access Premium Features", "User with active subscription");
            await SeedPermissionIfNotExists(context, "premium.manage", "Manage Premium Subscriptions", "Admin can manage subscriptions");
            await SeedPermissionIfNotExists(context, "ai_features.access", "Access AI Features", "Premium users can use AI features");
            await SeedPermissionIfNotExists(context, "reports.advanced", "Generate Advanced Reports", "Premium users can generate advanced reports");
            await SeedPermissionIfNotExists(context, "data.export", "Export Personal Data", "Premium users can export their data");
            await SeedPermissionIfNotExists(context, "notifications.push", "Push Notifications", "Premium users receive push notifications");

            // Subscription permissions
            await SeedPermissionIfNotExists(context, "subscription.purchase", "Purchase Subscription", "User can purchase a premium subscription");
            await SeedPermissionIfNotExists(context, "subscription.read", "Read Subscription", "User can view their subscription status and history");
            await SeedPermissionIfNotExists(context, "subscription.read_all", "Read All Subscriptions", "Admin can view all subscription transactions");

            // Week 3 - Pregnancy Core Permissions (used by controllers)
            await SeedPermissionIfNotExists(context, "pregnancy.read", "Read Pregnancy", "User can read their own pregnancy data");
            await SeedPermissionIfNotExists(context, "pregnancy.write", "Write Pregnancy", "User can create/update their own pregnancy");
            await SeedPermissionIfNotExists(context, "pregnancy.delete", "Delete Pregnancy", "User can delete their own pregnancy");
            await SeedPermissionIfNotExists(context, "pregnancy.condition.read", "Read Pregnancy Conditions", "User can view pregnancy conditions");
            await SeedPermissionIfNotExists(context, "pregnancy.condition.write", "Write Pregnancy Conditions", "User can add/update pregnancy conditions");
            await SeedPermissionIfNotExists(context, "pregnancy.condition.delete", "Delete Pregnancy Conditions", "User can remove pregnancy conditions");
            await SeedPermissionIfNotExists(context, "pregnancy.visit.read", "Read Prenatal Visits", "User can view prenatal visits");
            await SeedPermissionIfNotExists(context, "pregnancy.visit.write", "Write Prenatal Visits", "User can create/update prenatal visits");
            await SeedPermissionIfNotExists(context, "pregnancy.visit.delete", "Delete Prenatal Visits", "User can delete prenatal visits");
            await SeedPermissionIfNotExists(context, "pregnancy.test.read", "Read Prenatal Tests", "User can view prenatal tests");
            await SeedPermissionIfNotExists(context, "pregnancy.test.write", "Write Prenatal Tests", "User can create/update prenatal tests");
            await SeedPermissionIfNotExists(context, "pregnancy.test.delete", "Delete Prenatal Tests", "User can delete prenatal tests");

            // Week 7 — Nutrition + Meal Planning
            await SeedPermissionIfNotExists(context, "food_preference.read", "Read Food Preferences", "User can view their food preferences and allergies");
            await SeedPermissionIfNotExists(context, "food_preference.write", "Write Food Preferences", "User can add/update food preferences and allergies");
            await SeedPermissionIfNotExists(context, "food_preference.delete", "Delete Food Preferences", "User can remove food preferences");
            await SeedPermissionIfNotExists(context, "nutrition_note.read", "Read Nutrition Notes", "User can view their nutrition notes");
            await SeedPermissionIfNotExists(context, "nutrition_note.write", "Write Nutrition Notes", "User can add/update nutrition notes");
            await SeedPermissionIfNotExists(context, "nutrition_note.delete", "Delete Nutrition Notes", "User can remove nutrition notes");
            await SeedPermissionIfNotExists(context, "meal_plan.read", "Read Meal Plans", "User can view their meal plans");
            await SeedPermissionIfNotExists(context, "meal_plan.generate", "Generate Meal Plan", "User can generate AI meal plans");
            await SeedPermissionIfNotExists(context, "meal_plan.delete", "Delete Meal Plans", "User can delete their meal plans");
            await SeedPermissionIfNotExists(context, "recipe.read", "Read Recipes", "User can view recipes in their meal plans");
            await SeedPermissionIfNotExists(context, "meal_plan_feedback.write", "Write Meal Plan Feedback", "User can rate meal plans");
            await SeedPermissionIfNotExists(context, "meal_item_feedback.write", "Write Meal Item Feedback", "User can like/dislike meal items");

            await context.SaveChangesAsync();

            // Load all permissions from database
            var permissions = await context.Permissions.ToListAsync();

            // ===========================
            // 3. Assign Permissions to Roles (Idempotent)
            // ===========================

            // ADMIN - All permissions
            var adminPermissions = permissions.Select(p => p.Id).ToList();
            await AssignPermissionsToRole(context, adminRole.Id, adminPermissions);

            // USER - Basic permissions for pregnant mothers
            var userPermissionCodes = new[]
            {
                "user_profiles.write.own", "doctor_profiles.read",
                "pregnancies.write.own",
                "pregnancy.read", "pregnancy.write", "pregnancy.delete",
                "pregnancy.condition.read", "pregnancy.condition.write", "pregnancy.condition.delete",
                "pregnancy.visit.read", "pregnancy.visit.write", "pregnancy.visit.delete",
                "pregnancy.test.read", "pregnancy.test.write", "pregnancy.test.delete",
                "document.create", "document.view", "document.update", "document.delete", "document.favorite",
                "ocr.trigger", "ocr.view",
                "ocr.review", "ocr.confirm",
                "weight_logs.write.own", "meal_plans.write.own",
                "weight_log.read", "weight_log.write", "weight_log.delete",
                "weight_goal.read", "weight_goal.write",
                "weight_alert.read", "weight_alert.resolve",
                "reminders.write.own",
                "consults.request", "chat.send", "calls.join",
                "subscription.purchase", "subscription.read",
                // Week 7 — Nutrition
                "food_preference.read", "food_preference.write", "food_preference.delete",
                "nutrition_note.read", "nutrition_note.write", "nutrition_note.delete",
                "meal_plan.read", "meal_plan.generate", "meal_plan.delete",
                "recipe.read",
                "meal_plan_feedback.write", "meal_item_feedback.write"
            };
            var userPermissionIds = permissions.Where(p => userPermissionCodes.Contains(p.Code)).Select(p => p.Id).ToList();
            await AssignPermissionsToRole(context, userRole.Id, userPermissionIds);

            // DOCTOR - Medical + Profile permissions
            var doctorPermissionCodes = new[]
            {
                "user_profiles.write.own", "doctor_profiles.read", "doctor_profiles.write.own",
                "pregnancies.write.own", "weight_logs.write.own", "meal_plans.write.own", 
                "reminders.write.own",
                "pregnancy.read", "pregnancy.write", "pregnancy.delete",
                "pregnancy.condition.read", "pregnancy.condition.write", "pregnancy.condition.delete",
                "pregnancy.visit.read", "pregnancy.visit.write", "pregnancy.visit.delete",
                "pregnancy.test.read", "pregnancy.test.write", "pregnancy.test.delete",
                "document.create", "document.view", "document.update", "document.delete", "document.favorite",
                "ocr.trigger", "ocr.view",
                "ocr.review", "ocr.confirm",
                "pregnancies.read.any", "pregnancies.update.any", "pregnancy_conditions.write.any",
                "prenatal_visits.write.any", "prenatal_tests.write.any",
                "documents.read.any", "medical_data.export",
                "weight_logs.read.any", "meal_plans.read.any",
                "weight_log.read", "weight_log.write", "weight_log.delete",
                "weight_goal.read", "weight_goal.write",
                "weight_alert.read", "weight_alert.resolve",
                "availability.write.own", "consults.request", "consults.accept", "consults.view.assigned",
                "chat.send", "chat.participants.manage", "calls.join", "calls.recordings.access",
                "reminders.manage.any",
                "premium.access", "ai_features.access", "reports.advanced",
                "subscription.purchase", "subscription.read",
                // Week 7 — Nutrition (same as USER except meal_plan.generate)
                "food_preference.read", "food_preference.write", "food_preference.delete",
                "nutrition_note.read", "nutrition_note.write", "nutrition_note.delete",
                "meal_plan.read", "meal_plan.delete",
                "recipe.read",
                "meal_plan_feedback.write", "meal_item_feedback.write"
            };
            var doctorPermissionIds = permissions.Where(p => doctorPermissionCodes.Contains(p.Code)).Select(p => p.Id).ToList();
            await AssignPermissionsToRole(context, doctorRole.Id, doctorPermissionIds);

            // PREMIUM - Premium features for subscribed users
            var premiumPermissionCodes = new[]
            {
                "premium.access", "ai_features.access", "reports.advanced",
                "data.export", "notifications.push"
            };
            var premiumPermissionIds = permissions.Where(p => premiumPermissionCodes.Contains(p.Code)).Select(p => p.Id).ToList();
            await AssignPermissionsToRole(context, premiumRole.Id, premiumPermissionIds);

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Helper method to seed permission if not exists
        /// </summary>
        private static async Task SeedPermissionIfNotExists(AppDbContext context, string code, string name, string description)
        {
            if (!await context.Permissions.AnyAsync(p => p.Code == code))
            {
                await context.Permissions.AddAsync(new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Description = description
                });
            }
        }

        /// <summary>
        /// Helper method to assign permissions to role (idempotent)
        /// </summary>
        private static async Task AssignPermissionsToRole(AppDbContext context, Guid roleId, List<Guid> permissionIds)
        {
            foreach (var permissionId in permissionIds)
            {
                if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    });
                }
            }
        }
    }
}
