# RBAC Implementation Guide

## 📋 Overview
Hệ thống RBAC (Role-Based Access Control) đã được implement đầy đủ với:
- ✅ 3 Roles: ADMIN, USER, DOCTOR
- ✅ 60+ Permissions (theo module)
- ✅ Authorization Policies
- ✅ Seed Data
- ✅ Controllers & Services

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

# Workflow
weight_logs.read.any
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

## 🚀 Cách sử dụng

### **1. Run Migration**
```bash
cd src/FPT.EXE201.Api
dotnet ef migrations add Init-RBAC-Seed --project ../FPT.EXE201.Infrastructure
dotnet ef database update
```

### **2. Seed Data tự động**
Khi app chạy lần đầu, seed data sẽ tự động được thêm vào database.

### **3. Sử dụng Permission Authorization**

#### **Controller level:**
```csharp
[RequirePermission("users.read.any")]
public async Task<IActionResult> GetAllUsers() { }
```

#### **Check trong Service:**
```csharp
public async Task<bool> CanEdit(Guid userId, Guid resourceId)
{
    return await _userRoleService.HasPermissionAsync(userId, "pregnancies.update.any");
}
```

#### **Ownership pattern:**
```csharp
var pregnancy = await _repo.GetByIdAsync(id);
if (pregnancy.UserId != currentUserId && !HasPermission("pregnancies.read.any"))
    throw new ForbiddenException();
```

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

## 🔧 Cấu hình thêm Permission

### **Thêm permission mới vào seed:**
```csharp
// Trong DatabaseSeeder.cs
new Permission
{
    Id = Guid.NewGuid(),
    Code = "your_module.action",
    Name = "Your Action Name",
    Description = "Description here"
}
```

### **Gán permission cho role:**
```csharp
// Trong section assign permissions
var doctorPermissionCodes = new[]
{
    // ... existing permissions
    "your_module.action"
};
```

---

## ⚠️ Lưu ý quan trọng

1. **System Roles không thể xóa/sửa code:**
   - ADMIN, USER, DOCTOR được protect

2. **Permission naming convention:**
   - Pattern: `{module}.{action}.{scope}`
   - Example: `pregnancies.read.any`, `user_profiles.write.own`

3. **Ownership vs Permission:**
   - USER: chỉ own data (enforce bằng UserId check)
   - DOCTOR: có `.any` permissions để cross-user
   - ADMIN: toàn quyền

4. **Premium features:**
   - Check `premium.access` permission
   - Gán khi user subscribe

---

## 📚 Files đã tạo

### **Domain**
- ✅ `Role.cs`, `Permission.cs`, `RolePermission.cs`, `UserRole.cs` (đã có sẵn)

### **Application Layer**
- ✅ `IRepositories/IRoleRepository.cs`
- ✅ `IRepositories/IPermissionRepository.cs`
- ✅ `IRepositories/IUserRoleRepository.cs`
- ✅ `IServices/IRoleService.cs`
- ✅ `IServices/IPermissionService.cs`
- ✅ `IServices/IUserRoleService.cs`
- ✅ `Services/RoleService.cs`
- ✅ `Services/PermissionService.cs`
- ✅ `Services/UserRoleService.cs`
- ✅ `DTOs/RBAC/*Dto.cs`
- ✅ `Validations/RBAC/*Validator.cs`
- ✅ `Authorization/PermissionRequirement.cs`
- ✅ `Authorization/PermissionAuthorizationHandler.cs`
- ✅ `Authorization/RequirePermissionAttribute.cs`
- ✅ `Authorization/PermissionPolicyProvider.cs`

### **Infrastructure Layer**
- ✅ `Repositories/RoleRepository.cs`
- ✅ `Repositories/PermissionRepository.cs`
- ✅ `Repositories/UserRoleRepository.cs`
- ✅ `Persistence/DatabaseSeeder.cs`
- ✅ `MapperConfigs/RBACMapperProfile.cs`
- ✅ `Configurations/*Configuration.cs` (đã có sẵn)

### **API Layer**
- ✅ `Controllers/RolesController.cs`
- ✅ `Controllers/PermissionsController.cs`
- ✅ `Controllers/UserRolesController.cs`

---

## ✅ Next Steps

1. **Run migration và test API**
2. **Tạo admin user đầu tiên:**
   ```csharp
   // Sau khi register user đầu tiên, gán ADMIN role manually
   INSERT INTO user_roles (user_id, role_id, created_at)
   SELECT '{user_id}', id, NOW() FROM roles WHERE code = 'ADMIN';
   ```

3. **Implement ownership checks trong các module khác**
4. **Add premium subscription logic**
5. **Week 3-12: Implement các modules theo roadmap**

---

**Hoàn thành! 🎉**
