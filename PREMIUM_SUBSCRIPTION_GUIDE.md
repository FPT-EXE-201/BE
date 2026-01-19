# Premium Subscription Implementation Guide

## 📋 Chiến lược Premium

### **Đã implement:**
✅ Role `PREMIUM` với 5 permissions:
- `premium.access` - Truy cập premium features
- `ai_features.access` - AI nutrition suggestions
- `reports.advanced` - Advanced analytics reports
- `data.export` - Export personal data
- `notifications.push` - Push notifications

### **Cách hoạt động:**

```
USER mặc định: Role "USER" (permissions cơ bản)
↓
USER mua Premium
↓
Gán thêm role "PREMIUM" (user có 2 roles: USER + PREMIUM)
↓
User có tất cả permissions của cả 2 roles
↓
Subscription hết hạn → Xóa role "PREMIUM"
```

---

## 🔧 Implementation Steps

### **1. Tạo Subscription Entity & Table**

```csharp
// Domain/Entities/Subscription.cs
public class Subscription : BaseEntity
{
    public Guid UserId { get; set; }
    public string Plan { get; set; } = null!; // "MONTHLY", "YEARLY"
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!; // "ACTIVE", "EXPIRED", "CANCELLED"
    public string? PaymentProvider { get; set; } // "STRIPE", "PAYPAL", etc
    public string? PaymentTransactionId { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
```

### **2. Subscription Service**

```csharp
// Application/IServices/ISubscriptionService.cs
public interface ISubscriptionService
{
    Task<bool> ActivatePremiumAsync(Guid userId, string plan, CancellationToken ct = default);
    Task<bool> DeactivatePremiumAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsPremiumActiveAsync(Guid userId, CancellationToken ct = default);
    Task CheckExpiredSubscriptionsAsync(CancellationToken ct = default);
}

// Application/Services/SubscriptionService.cs
public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRoleService _userRoleService;

    public async Task<bool> ActivatePremiumAsync(Guid userId, string plan, CancellationToken ct = default)
    {
        // 1. Tạo subscription record
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Plan = plan,
            Price = plan == "MONTHLY" ? 99000 : 990000,
            StartDate = DateTime.UtcNow,
            EndDate = plan == "MONTHLY" 
                ? DateTime.UtcNow.AddMonths(1) 
                : DateTime.UtcNow.AddYears(1),
            Status = "ACTIVE"
        };
        
        await _unitOfWork.Subscriptions.AddAsync(subscription, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 2. Gán role PREMIUM cho user
        var premiumRole = await _unitOfWork.Roles.GetByCodeAsync("PREMIUM", ct: ct);
        if (premiumRole == null)
            throw new NotFoundException("Premium role not found");

        await _userRoleService.AssignRolesToUserAsync(userId, new List<Guid> { premiumRole.Id }, ct);
        
        return true;
    }

    public async Task<bool> DeactivatePremiumAsync(Guid userId, CancellationToken ct = default)
    {
        // 1. Update subscription status
        var subscription = await _unitOfWork.Subscriptions.GetActiveByUserIdAsync(userId, ct);
        if (subscription != null)
        {
            subscription.Status = "EXPIRED";
            _unitOfWork.Subscriptions.Update(subscription);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // 2. Xóa role PREMIUM
        var premiumRole = await _unitOfWork.Roles.GetByCodeAsync("PREMIUM", ct: ct);
        if (premiumRole != null)
        {
            await _userRoleService.RemoveRoleFromUserAsync(userId, premiumRole.Id, ct);
        }
        
        return true;
    }

    public async Task<bool> IsPremiumActiveAsync(Guid userId, CancellationToken ct = default)
    {
        // Check nếu user có role PREMIUM
        return await _userRoleService.HasPermissionAsync(userId, "premium.access", ct);
    }

    // Background job - chạy mỗi ngày để check expired subscriptions
    public async Task CheckExpiredSubscriptionsAsync(CancellationToken ct = default)
    {
        var expiredSubs = await _unitOfWork.Subscriptions.GetExpiredAsync(ct);
        
        foreach (var sub in expiredSubs)
        {
            await DeactivatePremiumAsync(sub.UserId, ct);
        }
    }
}
```

### **3. Payment Controller**

```csharp
// Controllers/SubscriptionsController.cs
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;

    [HttpPost("purchase")]
    public async Task<IActionResult> PurchasePremium([FromBody] PurchaseDto dto)
    {
        var userId = GetCurrentUserId();
        
        // 1. Process payment (Stripe/PayPal/VNPay)
        // var paymentResult = await _paymentService.ProcessAsync(dto);
        
        // 2. Activate premium
        await _subscriptionService.ActivatePremiumAsync(userId, dto.Plan);
        
        return Success<object>(null, "Premium activated successfully");
    }

    [HttpDelete("cancel")]
    public async Task<IActionResult> CancelSubscription()
    {
        var userId = GetCurrentUserId();
        await _subscriptionService.DeactivatePremiumAsync(userId);
        return NoContentResponse();
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetCurrentUserId();
        var isPremium = await _subscriptionService.IsPremiumActiveAsync(userId);
        
        return Success(new { isPremium });
    }
}
```

---

## 🎯 Usage Examples

### **Frontend check premium:**
```typescript
// After login, check permissions
const userPermissions = loginResponse.permissions;
const isPremium = userPermissions.includes('premium.access');

// Show/hide premium features
if (isPremium) {
  showAIFeatures();
  showAdvancedReports();
}
```

### **Backend check trong controller:**
```csharp
[HttpPost("generate-ai-meal-plan")]
[RequirePermission("ai_features.access")] // ← Chỉ premium users
public async Task<IActionResult> GenerateAIMealPlan()
{
    // AI logic here
}
```

### **Backend check trong service:**
```csharp
public async Task<MealPlanDto> GenerateMealPlanAsync(Guid userId)
{
    var isPremium = await _userRoleService.HasPermissionAsync(userId, "ai_features.access");
    
    if (!isPremium)
        throw new ForbiddenException("AI features require premium subscription");
    
    // Generate meal plan with AI...
}
```

---

## ⚡ Background Jobs (Optional)

### **Daily check expired subscriptions:**

```csharp
// Use Hangfire or built-in BackgroundService
public class SubscriptionExpiryJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await _subscriptionService.CheckExpiredSubscriptionsAsync(ct);
            await Task.Delay(TimeSpan.FromHours(24), ct); // Check daily
        }
    }
}
```

---

## 📊 Database Structure

```sql
-- USER table (existing)
-- ROLES table: USER, ADMIN, DOCTOR, PREMIUM
-- USER_ROLES table: many-to-many

-- USER có role USER + PREMIUM (2 records trong user_roles)
user_id | role_id (USER)    | created_at
user_id | role_id (PREMIUM) | created_at

-- SUBSCRIPTIONS table (new)
CREATE TABLE subscriptions (
  id CHAR(36) PRIMARY KEY,
  user_id CHAR(36) NOT NULL,
  plan VARCHAR(20) NOT NULL,
  price DECIMAL(10,2) NOT NULL,
  start_date DATETIME(3) NOT NULL,
  end_date DATETIME(3) NOT NULL,
  status VARCHAR(20) NOT NULL,
  payment_provider VARCHAR(50),
  payment_transaction_id VARCHAR(255),
  created_at DATETIME(3) DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) DEFAULT CURRENT_TIMESTAMP(3),
  FOREIGN KEY (user_id) REFERENCES users(id)
);
```

---

## ✅ Benefits của approach này:

1. **Flexible**: User có thể có nhiều roles (USER + PREMIUM + DOCTOR nếu cần)
2. **Automatic permission**: Khi có role PREMIUM → tự động có 5 premium permissions
3. **Easy revoke**: Xóa role PREMIUM → mất ngay premium permissions
4. **No code change**: Chỉ cần assign/remove role, không sửa code logic
5. **Audit trail**: User roles có timestamps → biết khi nào subscribe/unsubscribe

---

**Next Steps:**
1. ✅ Role PREMIUM đã được seed
2. ⏳ Tạo Subscription entity & migration
3. ⏳ Implement SubscriptionService
4. ⏳ Integrate payment gateway (Stripe/VNPay)
5. ⏳ Setup background job check expiry
