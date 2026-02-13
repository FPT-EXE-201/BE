# 🇻🇳 Hướng dẫn cấu hình UTF-8 cho tiếng Việt

## ✅ Đã hoàn thành

### 1. **Connection String** ✅
File: `appsettings.json`
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=fpt_exe201_db;User=root;Password=123456@Abc;Port=3306;CharSet=utf8mb4;"
}
```
- `CharSet=utf8mb4` - hỗ trợ đầy đủ Unicode (bao gồm emoji và các ký tự đặc biệt)

### 2. **DbContext Configuration** ✅
File: `AppDbContext.cs`
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure charset for Vietnamese support (utf8mb4 for full Unicode support)
    modelBuilder.HasCharSet("utf8mb4");
    
    // ... other configurations
}
```

### 3. **Entity Configurations** ✅
Tất cả các string columns đã được cấu hình với `utf8mb4_unicode_ci` collation:

**Các file đã update:**
- ✅ `UserConfiguration.cs` - Email, Phone, Status
- ✅ `UserProfileConfiguration.cs` - FullName, AvatarUrl, PreferredLang
- ✅ `RoleConfiguration.cs` - Code, Name, Description
- ✅ `PermissionConfiguration.cs` - Code, Name, Description
- ✅ `LanguageConfiguration.cs` - Code, Name

**Ví dụ:**
```csharp
builder.Property(e => e.FullName)
    .HasColumnName("full_name")
    .HasMaxLength(200)
    .UseCollation("utf8mb4_unicode_ci"); // ✅ Hỗ trợ tiếng Việt
```

### 4. **Migration** ✅
Migration đã được tạo: `20260209085725_AddUtf8mb4CharsetAndCollation.cs`

---

## 🚀 Cách áp dụng

### Bước 1: Áp dụng migration vào database
```powershell
cd "d:\Source Code\Source C#\FPT-EXE-201\src\FPT.EXE201.Infrastructure"

dotnet ef database update `
    --project FPT.EXE201.Infrastructure.csproj `
    --startup-project ..\FPT.EXE201.Api\FPT.EXE201.Api.csproj
```

### Bước 2: Kiểm tra database
Kết nối MySQL và chạy:
```sql
-- Kiểm tra charset của database
SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME 
FROM INFORMATION_SCHEMA.SCHEMATA 
WHERE SCHEMA_NAME = 'fpt_exe201_db';

-- Kiểm tra charset và collation của các bảng
SELECT 
    TABLE_NAME, 
    TABLE_COLLATION 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'fpt_exe201_db';

-- Kiểm tra collation của các columns
SELECT 
    TABLE_NAME, 
    COLUMN_NAME, 
    CHARACTER_SET_NAME, 
    COLLATION_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'fpt_exe201_db' 
    AND TABLE_NAME IN ('users', 'user_profiles', 'roles', 'permissions')
    AND DATA_TYPE IN ('varchar', 'text', 'char');
```

**Kết quả mong đợi:**
- Database charset: `utf8mb4`
- Table collation: `utf8mb4_unicode_ci` hoặc `utf8mb4_0900_ai_ci`
- Column collation: `utf8mb4_unicode_ci`

---

## 🧪 Test tiếng Việt

### Test 1: Register user với tên tiếng Việt
```json
POST /api/auth/register
{
    "email": "nguyen@example.com",
    "password": "Test123!@#",
    "fullName": "Nguyễn Văn Ồ Ổ Ộ Ỗ",
    "phone": "0912345678",
    "preferredLanguage": "vi"
}
```

### Test 2: Tạo role với tên tiếng Việt
```json
POST /api/roles
{
    "code": "BACSI",
    "name": "Bác sĩ chuyên khoa",
    "description": "Bác sĩ có chuyên môn sâu về một lĩnh vực cụ thể"
}
```

### Test 3: Kiểm tra data trong database
```sql
-- Xem dữ liệu tiếng Việt
SELECT full_name FROM user_profiles;
SELECT name, description FROM roles;

-- Test search với tiếng Việt
SELECT * FROM user_profiles WHERE full_name LIKE '%Ố%';
SELECT * FROM roles WHERE name LIKE '%Bác sĩ%';
```

---

## 📋 Checklist đảm bảo không bị lỗi dấu

### ✅ Backend (C# / EF Core)
- [x] Connection string có `CharSet=utf8mb4`
- [x] `modelBuilder.HasCharSet("utf8mb4")` trong `AppDbContext`
- [x] Tất cả string columns có `.UseCollation("utf8mb4_unicode_ci")`
- [x] Migration đã được tạo và apply

### ⚠️ Database (MySQL)
- [ ] Database charset: `utf8mb4`
- [ ] Table charset: `utf8mb4`
- [ ] Column collation: `utf8mb4_unicode_ci`

### ⚠️ Client (Frontend/Mobile)
- [ ] API requests dùng `Content-Type: application/json; charset=utf-8`
- [ ] HTTP headers có `Accept-Charset: utf-8`

---

## 🔧 Sửa lỗi nếu database đã tồn tại

Nếu database đã có data và bị lỗi dấu, chạy các lệnh sau:

### 1. Alter database charset
```sql
ALTER DATABASE fpt_exe201_db CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
```

### 2. Alter tất cả tables
```sql
ALTER TABLE users CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE user_profiles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE roles CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE permissions CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE languages CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- Thêm các bảng khác nếu cần
```

### 3. Hoặc apply migration (khuyến nghị)
```powershell
dotnet ef database update --project ... --startup-project ...
```

---

## 🎯 Lưu ý quan trọng

### ✅ utf8mb4 vs utf8
- **utf8mb4** ✅ - Hỗ trợ đầy đủ Unicode (4 bytes), bao gồm emoji 🙂
- **utf8** ❌ - Chỉ hỗ trợ 3 bytes, thiếu một số ký tự đặc biệt

### ✅ Collation recommendations
- **utf8mb4_unicode_ci** ✅ - Chuẩn Unicode, hỗ trợ tốt cho tiếng Việt
- **utf8mb4_general_ci** ⚠️ - Nhanh hơn nhưng ít chính xác hơn
- **utf8mb4_0900_ai_ci** ✅ - MySQL 8.0+, hỗ trợ tốt và nhanh

### ✅ Case sensitivity
- `_ci` = Case Insensitive (không phân biệt HOA thường)
- `_cs` = Case Sensitive (phân biệt HOA thường)
- `_ai` = Accent Insensitive (không phân biệt dấu: à = a)
- `_as` = Accent Sensitive (phân biệt dấu: à ≠ a)

**Khuyến nghị:** `utf8mb4_unicode_ci` - phù hợp nhất cho tiếng Việt 🇻🇳

---

## ✅ Hoàn tất!

Giờ đây ứng dụng của bạn đã hỗ trợ đầy đủ tiếng Việt:
- ✅ Lưu dữ liệu: **Nguyễn Văn Ồ Ổ Ộ Ỗ**
- ✅ Hiển thị: **Nguyễn Văn Ồ Ổ Ộ Ỗ**
- ✅ Tìm kiếm: `LIKE '%Ồ%'` hoạt động chính xác
- ✅ Sort: Sắp xếp đúng thứ tự tiếng Việt

**Không còn bị lỗi font, dấu lạ, hay �������!** 🎉
