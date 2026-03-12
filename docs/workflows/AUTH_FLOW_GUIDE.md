# Authentication & Authorization Flow Guide

> **Mục đích**: Mô tả chính xác auth flow đã implement, để AI code tiếp không sai approach.  
> **Cập nhật**: 2026-02-13 · Đồng bộ với codebase thực  
> **Xem thêm**: `DEVELOPMENT_WORKFLOW_GUIDE.md` §5 (Authorization Patterns), `RBAC_IMPLEMENTATION_GUIDE.md`

---

## 1. APPROACH ĐANG DÙNG

**Approach 2 — Permissions nhúng trong JWT Claims, KHÔNG query DB mỗi request.**

```
Login/Register → Query permissions 1 lần → Gắn vào JWT claims
→ Mỗi request sau đó chỉ đọc claims → KHÔNG query DB
```

| Đặc điểm | Giá trị |
|-----------|---------|
| Access Token lifetime | Configurable via `Jwt:ExpirationMinutes` (default 60 min) |
| Refresh Token lifetime | Configurable via `Jwt:RefreshTokenExpirationDays` (default 30 days) |
| Permission update | Có hiệu lực khi refresh token hoặc re-login |
| Token reuse detection | ✅ Có — revoke toàn bộ chain nếu phát hiện reuse |

---

## 2. AUTH FLOWS

### 2.1 Register Flow

```
POST /api/auth/register
{ "email", "password", "fullName", "phone?", "preferredLanguage?" }
    ↓
AuthService.RegisterAsync()
    1. Normalize email → lowercase
    2. Check email exists → ConflictException nếu trùng
    3. Validate language → fallback "vi"
    4. Hash password (IPasswordHasher)
    5. Create User + UserProfile (AutoMapper từ RegisterRequestDto)
    6. SaveChanges
    7. Query permissions + roles (1 lần)
    8. Issue RefreshToken → lưu DB (hash SHA-256)
    9. GenerateAccessTokenWithPermissions(user, permissions, roles, refreshTokenId)
    ↓
Response: AuthResponseDto { AccessToken, RefreshToken, TokenType, ExpiresIn, User }
```

### 2.2 Login Flow

```
POST /api/auth/login
{ "emailOrPhone": "user@email.com", "password": "..." }
    ↓
AuthService.LoginAsync()
    1. Detect email vs phone (contains "@")
    2. Query user (includeProfile: true, includeDeleted: false)
    3. Validate user exists → UnauthorizedException
    4. Check user.Status == Active → UnauthorizedException nếu không
    5. Verify password → UnauthorizedException
    6. Update LastLoginAt
    7. Query permissions + roles (1 lần)
    8. Issue RefreshToken → lưu DB
    9. GenerateAccessTokenWithPermissions
    ↓
Response: AuthResponseDto
```

### 2.3 Refresh Token Flow (Rotation)

```
POST /api/auth/refresh
{ "refreshToken": "base64..." }
    ↓
AuthService.RefreshTokenAsync()
    1. JwtTokenService.RotateRefreshTokenAsync():
       a. Hash token → tìm trong DB
       b. Check expired → UnauthorizedException
       c. ⚠️ Check revoked → REUSE DETECTION:
          Nếu đã revoked → revoke TOÀN BỘ chain → throw
       d. Load user
       e. Revoke token cũ (set RevokedAt)
       f. Issue token mới (link RotatedFromId → token cũ)
    2. Query latest permissions + roles
    3. GenerateAccessTokenWithPermissions với token mới
    ↓
Response: RefreshTokenResponseDto { AccessToken (MỚI), RefreshToken (MỚI), ... }
```

### 2.4 Logout Flow

```
POST /api/auth/logout
Headers: Authorization: Bearer <accessToken>
    ↓
AuthService.LogoutAsync(userId, refreshTokenId)
    - refreshTokenId lấy từ JWT claim "rtid"
    - Revoke refresh token by ID
    ↓
Response: 200 OK
```

---

## 3. JWT CLAIMS STRUCTURE

```json
{
  "sub": "guid-user-id",
  "email": "user@example.com",
  "jti": "unique-token-id",
  "userId": "guid-user-id",
  "status": "Active",
  "phone": "0901234567",
  "rtid": "guid-refresh-token-id",
  "role": ["member", "premium_member"],
  "permissions": [
    "pregnancy.read",
    "pregnancy.write",
    "prenatal_visit.read",
    "prenatal_visit.write"
  ],
  "exp": 1737360000,
  "iss": "FPT.EXE201.Api",
  "aud": "FPT.EXE201.Client"
}
```

**Custom claims**:
- `userId` — duplicate of `sub`, dùng cho compatibility
- `status` — `UserStatus` enum as string
- `rtid` — Refresh Token ID, dùng cho logout
- `permissions` — mảng permission codes (multi-value claim)
- `role` — ClaimTypes.Role, dùng cho `[Authorize(Roles = "...")]`

---

## 4. AUTHORIZATION PIPELINE

```
Request → JWT Middleware (validate signature/expiry)
       → [RequirePermission("x")] attribute
       → PermissionPolicyProvider (dynamic policy: "Permission:x")
       → PermissionAuthorizationHandler:
            Read claims "permissions" → check Contains(x)
            ✅ Succeed → execute controller
            ❌ Fail → 403 Forbidden
```

### Key Files

| File | Layer | Vai trò |
|------|-------|---------|
| `RequirePermissionAttribute.cs` | Application/Authorization | `[RequirePermission("x")]` → Policy `"Permission:x"` |
| `PermissionPolicyProvider.cs` | Application/Authorization | Dynamic policy creation từ prefix `Permission:` |
| `PermissionRequirement.cs` | Application/Authorization | `IAuthorizationRequirement` chứa permission string |
| `PermissionAuthorizationHandler.cs` | Application/Authorization | Đọc claims, check Contains — KHÔNG query DB |
| `JwtTokenService.cs` | Infrastructure/Services | Generate tokens, refresh rotation, revocation |
| `AuthService.cs` | Application/Services | Login/Register/Refresh/Logout business logic |

### DI Registration (Infrastructure/DependencyInjection.cs)

```csharp
services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
services.AddScoped<IJwtTokenService, JwtTokenService>();
```

---

## 5. REFRESH TOKEN SECURITY

| Aspect | Implementation |
|--------|---------------|
| Storage | SHA-256 hash trong DB (`token_hash` BINARY(32)), KHÔNG lưu plaintext |
| Rotation | Mỗi lần refresh → token cũ revoke + token mới issue |
| Reuse detection | Nếu token đã revoke bị dùng lại → revoke toàn bộ chain |
| Chain tracking | `rotated_from_id` FK → link token mới với token cũ |
| Table | `auth_refresh_tokens` |
| Cleanup | Token expired không tự xóa — cần background job (future) |

---

## 6. AUTH DTOs (⚠️ NGOẠI LỆ — dùng class, KHÔNG record)

Auth DTOs dùng `class` thay vì `record` vì đã implement trước khi áp dụng record convention:

```csharp
// Request
public class LoginRequestDto
{
    public string EmailOrPhone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Response
public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserResponseDto User { get; set; } = default!;
}

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public string PreferredLanguage { get; set; } = "vi";
}
```

> **Ghi chú cho AI**: Khi implement module MỚI (Week 4+), luôn dùng `record` cho DTOs. Auth DTOs giữ nguyên `class` để không breaking change.

---

## 7. HELPER — GetCurrentUserId

```csharp
// BaseApiController.cs
protected Guid GetCurrentUserId()
{
    var userIdClaim = User.FindFirst("userId")?.Value
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        throw new UnauthorizedException("User ID not found in token");
    return userId;
}
```

Tất cả Controller kế thừa `BaseApiController` → gọi `GetCurrentUserId()` để lấy userId từ JWT claims.
