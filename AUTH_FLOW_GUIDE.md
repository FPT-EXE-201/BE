# Authentication & Authorization Flow Guide

## 🔐 2 Approaches cho Permission Checking

### **Approach 1: Claims chỉ có userId - Query DB mỗi request (HIỆN TẠI)**
### **Approach 2: Claims chứa permissions - Không query DB (FASTER)**

---

## ⚡ Approach 1: Query DB (Current Implementation)

### **Ưu điểm:**
- ✅ JWT token nhỏ gọn
- ✅ Permission thay đổi → hiệu lực ngay (không cần re-login)
- ✅ Revoke permission → user mất quyền ngay lập tức
- ✅ Phù hợp khi permissions thay đổi thường xuyên

### **Nhược điểm:**
- ❌ Query DB mỗi request có [RequirePermission]
- ❌ Tăng database load

### **Flow:**

```
1. USER LOGIN
   POST /api/auth/login
   { "email": "user@example.com", "password": "..." }
   ↓
2. AuthService.LoginAsync()
   - Validate credentials
   - Generate JWT with basic claims:
     * sub (UserId): "abc-123-..."
     * email: "user@example.com"
     * name: "John Doe"
     * jti: unique token ID
     * exp: expiration time
   ↓
3. Return JWT Token
   { "token": "eyJhbGc...", "user": {...} }

════════════════════════════════════════════════

4. USER REQUEST PROTECTED ENDPOINT
   GET /api/roles
   Headers: Authorization: Bearer eyJhbGc...
   ↓
5. JWT Authentication Middleware
   - Validate JWT signature
   - Extract claims → User.Identity populated
   - Claims available: userId, email, name
   ↓
6. [RequirePermission("rbac.roles.read")] Attribute
   - Creates Policy: "Permission:rbac.roles.read"
   ↓
7. PermissionPolicyProvider
   - Receives policy name "Permission:rbac.roles.read"
   - Extracts permission: "rbac.roles.read"
   - Creates PermissionRequirement("rbac.roles.read")
   ↓
8. PermissionAuthorizationHandler.HandleRequirementAsync()
   - Extract userId from User.Claims (sub)
   - 🔍 QUERY DB: UserRoleService.HasPermissionAsync(userId, "rbac.roles.read")
     → Query: UserRoles → RolePermissions → Permissions
     → Check if permission code exists
   - Return true/false
   ↓
9. Authorization Result
   - If true → Controller executes
   - If false → 403 Forbidden
```

### **Code Implementation:**

```csharp
// 1. Login - Generate JWT (JwtTokenService.cs)
public string GenerateToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ← UserId
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _jwtIssuer,
        audience: _jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// 2. Authorization Handler - Query DB
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserRoleService _userRoleService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Extract userId from JWT claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return; // Not authenticated
        }

        // 🔍 QUERY DATABASE for permissions
        var hasPermission = await _userRoleService.HasPermissionAsync(userId, requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}

// 3. UserRoleService.HasPermissionAsync - DB Query
public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken ct = default)
{
    var permissionCodes = await _unitOfWork.UserRoles.GetUserPermissionCodesAsync(userId, ct);
    return permissionCodes.Contains(permissionCode);
}

// SQL executed:
// SELECT DISTINCT p.code
// FROM user_roles ur
// JOIN roles r ON ur.role_id = r.id
// JOIN role_permissions rp ON r.id = rp.role_id
// JOIN permissions p ON rp.permission_id = p.id
// WHERE ur.user_id = @userId
```

---

## 🚀 Approach 2: Claims chứa Permissions (FASTER - RECOMMENDED)

### **Ưu điểm:**
- ✅ Không query DB mỗi request → NHANH HƠN
- ✅ Giảm database load đáng kể
- ✅ Offline validation (không cần DB connection)

### **Nhược điểm:**
- ❌ JWT token lớn hơn (có thể 2-5KB nếu nhiều permissions)
- ❌ Permission thay đổi → phải re-login để update
- ❌ Revoke permission → chỉ hiệu lực sau khi token expire

### **Flow:**

```
1. USER LOGIN
   POST /api/auth/login
   ↓
2. AuthService.LoginAsync()
   - Validate credentials
   - 🔍 QUERY DB 1 LẦN: Get user permissions
     → UserRoleService.GetUserPermissionCodesAsync(userId)
   - Generate JWT with permissions in claims:
     * sub: userId
     * email: email
     * permissions: ["users.read.any", "rbac.roles.read", ...]
   ↓
3. Return JWT Token (lớn hơn vì chứa permissions)

════════════════════════════════════════════════

4. USER REQUEST PROTECTED ENDPOINT
   GET /api/roles
   Headers: Authorization: Bearer eyJhbGc...
   ↓
5. JWT Authentication Middleware
   - Validate JWT signature
   - Extract claims (including permissions)
   ↓
6. PermissionAuthorizationHandler
   - Extract permissions from User.Claims
   - ❌ KHÔNG query DB
   - Check: claims.Contains("rbac.roles.read")
   ↓
7. Authorization Result
   - If true → Controller executes
   - If false → 403 Forbidden
```

### **Code Implementation:**

```csharp
// 1. Login - Include permissions in JWT (AuthService.cs)
public async Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
{
    // Validate user...
    var user = await ValidateCredentials(dto.Email, dto.Password);

    // 🔍 QUERY PERMISSIONS 1 LẦN
    var permissions = await _userRoleService.GetUserPermissionCodesAsync(user.Id, ct);
    var roles = await _userRoleService.GetUserRoleCodesAsync(user.Id, ct);

    // Generate token with permissions
    var token = _jwtTokenService.GenerateToken(user, permissions, roles);

    return new LoginResponseDto
    {
        Token = token,
        User = _mapper.Map<UserDto>(user),
        Permissions = permissions, // ← FE có thể dùng để show/hide UI
        Roles = roles
    };
}

// 2. JWT Service - Add permissions to claims
public string GenerateToken(User user, List<string> permissions, List<string> roles)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    // Add roles
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    // Add permissions (custom claim type)
    foreach (var permission in permissions)
    {
        claims.Add(new Claim("permissions", permission));
    }

    // Generate token...
    var token = new JwtSecurityToken(
        issuer: _jwtIssuer,
        audience: _jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// 3. Authorization Handler - Read from claims (NO DB QUERY)
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // ✅ Đọc permissions từ claims - KHÔNG QUERY DB
        var userPermissions = context.User.FindAll("permissions")
            .Select(c => c.Value)
            .ToList();

        if (userPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask; // ← No async, no DB query
    }
}
```

---

## 📊 So sánh Performance

### **Approach 1: Query DB**
```
Request 1: Query DB (50ms)
Request 2: Query DB (50ms)
Request 3: Query DB (50ms)
...
Request 100: Query DB (50ms)
Total: 5000ms for 100 requests
```

### **Approach 2: Claims**
```
Login: Query DB once (50ms) → Generate token
Request 1: Read claims (0.1ms)
Request 2: Read claims (0.1ms)
Request 3: Read claims (0.1ms)
...
Request 100: Read claims (0.1ms)
Total: 50ms (login) + 10ms (100 requests) = 60ms
```

**Speed up: ~83x faster!**

---

## 🎯 Recommendation

### **Use Approach 2 (Permissions in Claims) IF:**
- ✅ Permissions không thay đổi thường xuyên
- ✅ Cần performance cao (many requests/second)
- ✅ OK với việc user phải re-login khi permission thay đổi

### **Use Approach 1 (Query DB) IF:**
- ✅ Permissions thay đổi real-time
- ✅ Cần revoke permissions ngay lập tức
- ✅ Database có capacity xử lý load

---

## 💡 Hybrid Approach (BEST PRACTICE)

### **Combine cả 2:**

```csharp
// Option 1: Cache permissions trong memory
private readonly IMemoryCache _cache;

public async Task<bool> HasPermissionAsync(Guid userId, string permission)
{
    var cacheKey = $"user_permissions_{userId}";
    
    if (!_cache.TryGetValue(cacheKey, out List<string> permissions))
    {
        // Cache miss → Query DB
        permissions = await _unitOfWork.UserRoles.GetUserPermissionCodesAsync(userId);
        
        // Cache for 15 minutes
        _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(15));
    }
    
    return permissions.Contains(permission);
}

// Invalidate cache khi permission thay đổi
public async Task AssignRolesToUserAsync(Guid userId, List<Guid> roleIds)
{
    // Assign logic...
    
    // Invalidate cache
    _cache.Remove($"user_permissions_{userId}");
}
```

### **Hoặc: Short-lived JWT + Refresh Token**

```
- Access Token (JWT): 15 minutes, chứa permissions
- Refresh Token: 30 days, stored in DB
- Permission thay đổi → Wait max 15 mins để token expire
- Frontend auto-refresh token mỗi 15 mins
```

---

## ✅ Action Items

**Để implement Approach 2 (Recommended):**

1. ✅ Update `IJwtTokenService.GenerateToken()` để nhận permissions
2. ✅ Update `AuthService.LoginAsync()` để query permissions
3. ✅ Update `PermissionAuthorizationHandler` để đọc từ claims
4. ✅ Test với Swagger

**Code changes cần thiết:** ~3 files, ~30 lines code

---

## 🧪 Testing

### **Decode JWT để xem claims:**
```
Website: https://jwt.io/
Paste token → Xem payload:
{
  "sub": "abc-123-...",
  "email": "user@example.com",
  "permissions": [
    "users.read.any",
    "rbac.roles.read",
    ...
  ],
  "exp": 1737360000
}
```

### **Test Authorization:**
```csharp
// Controller
[HttpGet("test-permission")]
[RequirePermission("users.read.any")]
public IActionResult Test()
{
    var permissions = User.FindAll("permissions").Select(c => c.Value);
    return Ok(new { permissions });
}
```

---

**Bạn muốn implement Approach nào? Tôi sẽ viết code chi tiết!**
