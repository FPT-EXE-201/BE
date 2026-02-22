# RBAC Implementation Guide

> **Xem thêm**: `AUTH_FLOW_GUIDE.md` (JWT claims + auth pipeline), `DEVELOPMENT_WORKFLOW_GUIDE.md` §5 (authorization patterns)

## Overview
Hệ thống RBAC (Role-Based Access Control) đã được implement đầy đủ với:
- 3 Roles: ADMIN, USER, DOCTOR
- 60+ Permissions (theo module)
- Permission-based authorization via JWT claims (Approach 2 — KHÔNG query DB mỗi request)
- Seed Data
- Controllers & Services

---

## 🎯 Roles & Permissions

### **ADMIN** (Full permissions)
- Toàn quyền hệ thống
- Quản lý users, roles, permissions
- Access tất cả modules

### **USER** (Own data only)
```
user_profiles.write.own
doctor_profiles.read

# Week 4: Documents
document.create, document.view, document.update, document.delete, document.favorite

# Week 5: OCR + AI
ocr.trigger, ocr.view, ocr.review, ocr.confirm

# Week 6: Weight Tracking
weight_log.read, weight_log.write, weight_log.delete
weight_goal.read, weight_goal.write
weight_alert.read, weight_alert.resolve

+ premium permissions (nếu có subscription)
```

### **DOCTOR** (Medical + Cross-user)
```
# Profile
user_profiles.write.own
doctor_profiles.read
doctor_profiles.write.own

# Medical data
pregnancies.read.any
pregnancies.update.any
pregnancy_conditions.write.any
prenatal_visits.write.any
prenatal_tests.write.any

# Documents
documents.read.any
documents.ocr.rerun

# Weight Tracking (Week 6)
weight_log.read, weight_log.write, weight_log.delete
weight_goal.read, weight_goal.write
weight_alert.read, weight_alert.resolve

# Workflow
meal_plans.read.any
availability.write.own
consults.accept
consults.view.assigned
calls.recordings.access
medical_data.export

# Optional
premium.access
ai_features.access
```

---

## Cách sử dụng

### **1. Permission Check tại Controller (✅ CÁCH CHÍNH)**

Permissions được đọc từ JWT claims — KHÔNG query DB:

```csharp
[RequirePermission("users.read.any")]
public async Task<IActionResult> GetAllUsers() { }
```

### **2. Ownership Check tại Service**

```csharp
var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(id, ct);
if (pregnancy == null)
    throw new NotFoundException("Pregnancy not found");
if (pregnancy.UserId != currentUserId)
    throw new ForbiddenException("Access denied");
```

> **Lưu ý**: Service KHÔNG gọi `HasPermissionAsync()` để check permission. Permission check xảy ra ở Controller via `[RequirePermission]`. Service chỉ check ownership (UserId).

---

## 🔌 API Endpoints

### **Roles**
```
GET    /api/roles                    - Get all roles
GET    /api/roles/paged              - Get paged roles
GET    /api/roles/{id}               - Get role by ID
GET    /api/roles/code/{code}        - Get role by code
POST   /api/roles                    - Create role (Admin)
PUT    /api/roles/{id}               - Update role (Admin)
DELETE /api/roles/{id}               - Delete role (Admin)
GET    /api/roles/{id}/permissions   - Get role permissions
PUT    /api/roles/{id}/permissions   - Update role permissions (Admin)
```

### **Permissions**
```
GET    /api/permissions              - Get all permissions
GET    /api/permissions/paged        - Get paged permissions
GET    /api/permissions/{id}         - Get permission by ID
GET    /api/permissions/code/{code}  - Get permission by code
```

### **User Roles**
```
GET    /api/user-roles/me                     - Get my roles
GET    /api/user-roles/me/permissions         - Get my permissions
GET    /api/user-roles/users/{userId}         - Get user roles (Admin)
GET    /api/user-roles/users/{userId}/permissions - Get user permissions (Admin)
POST   /api/user-roles/users/{userId}/assign  - Assign roles (Admin)
DELETE /api/user-roles/users/{userId}/roles/{roleId} - Remove role (Admin)
PUT    /api/user-roles/users/{userId}/replace - Replace user roles (Admin)
```

---

## 📝 Testing với Swagger

### **1. Login để lấy token:**
```
POST /api/auth/login
{
  "email": "admin@example.com",
  "password": "YourPassword"
}
```

### **2. Authorize trong Swagger:**
Click **Authorize** button → Nhập: `Bearer {your_token}`

### **3. Test endpoints:**
- Thử GET /api/roles → nếu có permission sẽ thành công
- Thử POST /api/roles → chỉ ADMIN mới được

---

## Thêm permission mới vào seed

```csharp
// Trong DatabaseSeeder.cs — dùng anonymous type + fixed DateTime
private static readonly DateTime SeedDate = new(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc);

builder.Entity<Permission>().HasData(new
{
    Id = new Guid("..."),
    Code = "your_module.action",
    Name = "Your Action Name",
    Description = "Description here",
    CreatedAt = SeedDate,
    UpdatedAt = SeedDate
});
```

---

## Lưu ý quan trọng

1. **System Roles không thể xóa/sửa code:**
   ADMIN, USER, DOCTOR được protect

2. **Permission naming convention:**
   Pattern: `{module}.{action}` hoặc `{module}.{action}.{scope}`
   Example: `pregnancy.read`, `pregnancy.write`, `users.read.any`, `user_profiles.write.own`

3. **Ownership vs Permission:**
   - USER: chỉ own data (enforce bằng `UserId` check trong Service)
   - DOCTOR: có `.any` permissions để cross-user access
   - ADMIN: toàn quyền

4. **Permission update:** Khi admin assign/remove role, user phải re-login hoặc refresh token để JWT claims cập nhật permissions mới (xem `AUTH_FLOW_GUIDE.md` §2.3)

5. **Premium features:** Check `premium.access` permission — gán khi user subscribe

---

## Files đã implement (✅ = có trong codebase)

### **Domain**
- `Role.cs`, `Permission.cs`, `RolePermission.cs`, `UserRole.cs`

### **Application Layer**
- `IRepositories/IRoleRepository.cs`, `IPermissionRepository.cs`, `IUserRoleRepository.cs`
- `IServices/IRoleService.cs`, `IPermissionService.cs`, `IUserRoleService.cs`
- `Services/RoleService.cs`, `PermissionService.cs`, `UserRoleService.cs`
- `DTOs/RBAC/*Dto.cs`
- `Validations/RBAC/*Validator.cs`
- `Authorization/` — `PermissionRequirement.cs`, `PermissionAuthorizationHandler.cs`, `RequirePermissionAttribute.cs`, `PermissionPolicyProvider.cs`

### **Infrastructure Layer**
- `Repositories/` — `RoleRepository.cs`, `PermissionRepository.cs`, `UserRoleRepository.cs`
- `Persistence/DatabaseSeeder.cs`
- `MapperConfigs/RBACMapperProfile.cs`
- `Configurations/` — `RoleConfiguration.cs`, `PermissionConfiguration.cs`, `RolePermissionConfiguration.cs`, `UserRoleConfiguration.cs`

### **API Layer**
- `Controllers/` — `RolesController.cs`, `PermissionsController.cs`, `UserRolesController.cs`

---

> Xem `AUTH_FLOW_GUIDE.md` để hiểu chi tiết JWT claims structure và authorization pipeline.
