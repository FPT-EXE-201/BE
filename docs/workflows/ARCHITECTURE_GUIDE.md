# API Response Architecture Guide

> **Xem thêm**: `DEVELOPMENT_WORKFLOW_GUIDE.md` (conventions + patterns), `AUTH_FLOW_GUIDE.md` (auth pipeline)

## Tổng quan kiến trúc

Hệ thống sử dụng **Exception-based approach** để xử lý lỗi một cách nhất quán và đơn giản.

## 🏗️ Các thành phần chính

### 1. **ApiResponse** (Application Layer)
- **Mục đích**: Format chuẩn cho tất cả responses trả về client
- **Vị trí**: `FPT.EXE201.Application.DTOs.Common.ApiResponse`
- **Sử dụng**: Tự động được tạo bởi BaseApiController hoặc GlobalExceptionFilter

**Cấu trúc response:**
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "statusCode": 200,
  "data": { ... },
  "errors": null,
  "timestamp": "2026-01-11T10:30:00Z"
}
```

### 2. **Custom Exceptions** (Application Layer)
- **Mục đích**: Đại diện cho các lỗi business logic
- **Vị trí**: `FPT.EXE201.Application.Exceptions`
- **Sử dụng**: Service layer throw exceptions khi có lỗi

**Các exceptions có sẵn:**
- `NotFoundException` → 404
- `UnauthorizedException` → 401
- `ForbiddenException` → 403
- `ConflictException` → 409
- `BadRequestException` → 400 (có `Errors` list)
- `ValidationException` → 400 (có `Errors` từ FluentValidation)

### 3. **FluentValidation** (Auto-validation Pipeline)
- **Registration**: `AddFluentValidationAutoValidation()` trong `Program.cs`
- **Validators**: `Application/Validations/` — kế thừa `AbstractValidator<TDto>`
- **Auto-trigger**: ASP.NET ModelState tự chạy validator trước khi vào Controller action
- **Khi validation fail**: Trả 400 Bad Request với chi tiết lỗi — Controller KHÔNG cần validate thủ công

```csharp
// Ví dụ validator — tự chạy khi [FromBody] bind DTO
public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
```

### 4. **GlobalExceptionFilter** (API Layer)
- **Mục đích**: Tự động catch TẤT CẢ exceptions và convert thành ApiResponse
- **Vị trí**: `FPT.EXE201.Api.Filters.GlobalExceptionFilter`
- **Sử dụng**: Tự động, không cần code thêm

### 5. **BaseApiController** (API Layer)
- **Mục đích**: Cung cấp helper methods cho success responses + `GetCurrentUserId()`
- **Vị trí**: `FPT.EXE201.Api.Controllers.BaseApiController`
- **Sử dụng**: Tất cả controllers kế thừa từ đây

---

## ✅ Quy tắc sử dụng (QUAN TRỌNG!)

### 📦 **Trong Service Layer**

#### ✔️ Khi thành công:
```csharp
public async Task<UserDto> GetUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException($"User with ID {id} not found");
    
    return _mapper.Map<UserDto>(user);
}
```

#### ✔️ Khi có lỗi business:
```csharp
public async Task<ProductDto> CreateProductAsync(CreateProductDto request)
{
    // Validation
    if (string.IsNullOrWhiteSpace(request.Name))
        throw new BadRequestException("Product name is required");
    
    // Check duplicate
    var exists = await _repository.ExistsAsync(p => p.Name == request.Name);
    if (exists)
        throw new ConflictException("Product with this name already exists");
    
    // Create product
    var product = _mapper.Map<Product>(request);
    await _repository.AddAsync(product);
    await _unitOfWork.SaveChangesAsync();
    
    return _mapper.Map<ProductDto>(product);
}
```

#### ✔️ Khi cần authorization check:
```csharp
public async Task<OrderDto> GetOrderAsync(Guid orderId, Guid userId)
{
    var order = await _repository.GetByIdAsync(orderId);
    if (order == null)
        throw new NotFoundException("Order not found");
    
    if (order.UserId != userId)
        throw new ForbiddenException("You don't have permission to access this order");
    
    return _mapper.Map<OrderDto>(order);
}
```

---

### 🎮 **Trong Controller Layer**

#### ✔️ Pattern đơn giản (recommended):
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(Guid id)
{
    // Exceptions tự động được GlobalExceptionFilter catch
    var user = await _userService.GetUserAsync(id);
    return Success(user, "User retrieved successfully");
}

[HttpPost]
public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto request)
{
    var product = await _productService.CreateProductAsync(request);
    return Created(product, "Product created successfully");
}

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(Guid id)
{
    await _userService.DeleteUserAsync(id);
    return Success<object>(null, "User deleted successfully");
}
```

#### ✔️ Helper methods từ BaseApiController:
```csharp
// Success responses
Success<T>(data, message)           // 200 OK
Created<T>(data, message)           // 201 Created

// Error responses (chỉ dùng khi cần custom logic, thường không cần)
BadRequestResponse(message, errors) // 400
UnauthorizedResponse(message)       // 401
NotFoundResponse(message)           // 404
ConflictResponse(message)           // 409
```

---

## ❌ KHÔNG NÊN làm

### ❌ Không dùng try-catch trong controller:
```csharp
// ❌ KHÔNG LÀM NHƯ NÀY
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(Guid id)
{
    try 
    {
        var user = await _userService.GetUserAsync(id);
        return Success(user);
    }
    catch (NotFoundException ex)
    {
        return NotFoundResponse(ex.Message); // Thừa! GlobalExceptionFilter đã handle
    }
}
```

### ❌ Không tự tạo ApiResponse thủ công:
```csharp
// ❌ KHÔNG LÀM NHƯ NÀY
return Ok(new ApiResponse<UserDto> { ... }); // Phức tạp không cần thiết
```

---

## 🔄 Luồng xử lý

### 🎯 Success Flow:
```
Client Request 
  → Controller 
  → Service (return DTO) 
  → Controller (Success/Created helper) 
  → ApiResponse with data 
  → Client
```

### ⚠️ Error Flow:
```
Client Request 
  → Controller 
  → Service (throw Exception) 
  → GlobalExceptionFilter catches 
  → ApiResponse with error 
  → Client
```

---

## 📝 Examples đầy đủ

### Example 1: Simple GET
```csharp
// Service
public async Task<UserDto> GetUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException("User not found");
    return _mapper.Map<UserDto>(user);
}

// Controller
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(Guid id)
{
    var user = await _userService.GetUserAsync(id);
    return Success(user, "User retrieved successfully");
}
```

### Example 2: CREATE with validation
```csharp
// Service
public async Task<ProductDto> CreateProductAsync(CreateProductDto request)
{
    if (string.IsNullOrWhiteSpace(request.Name))
        throw new BadRequestException("Product name is required");
    
    var exists = await _repository.ExistsAsync(p => p.Name == request.Name);
    if (exists)
        throw new ConflictException("Product already exists");
    
    var product = _mapper.Map<Product>(request);
    await _repository.AddAsync(product);
    await _unitOfWork.SaveChangesAsync();
    return _mapper.Map<ProductDto>(product);
}

// Controller
[HttpPost]
public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto request)
{
    var product = await _productService.CreateProductAsync(request);
    return Created(product, "Product created successfully");
}
```

### Example 3: DELETE
```csharp
// Service
public async Task DeleteUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException("User not found");
    
    await _repository.SoftDeleteAsync(user);
    await _unitOfWork.SaveChangesAsync();
}

// Controller
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(Guid id)
{
    await _userService.DeleteUserAsync(id);
    return Success<object>(null, "User deleted successfully");
}
```

---

## 🎓 Kết luận

### ✅ Ưu điểm của approach này:
1. **Đơn giản**: Không cần try-catch ở controller
2. **Nhất quán**: Tất cả errors đều qua GlobalExceptionFilter
3. **Clean code**: Controller chỉ quan tâm happy path
4. **Dễ maintain**: Logic xử lý lỗi tập trung một nơi
5. **Type-safe**: Strongly typed với exceptions

### 📚 Nguyên tắc vàng:
1. **Service throw exceptions** khi có lỗi
2. **Service return DTOs** khi thành công
3. **Controller gọi service** và dùng helper methods
4. **GlobalExceptionFilter** tự động handle tất cả exceptions
5. **Không cần try-catch** trong controller (trừ trường hợp đặc biệt)

---

## Luồng xử lý tổng hợp (Validation + Exception)

```
Client Request
  → ASP.NET Model Binding
  → FluentValidation auto-validate → 400 nếu invalid (KHÔNG vào Controller)
  → Controller action
  → Service (throw Exception nếu lỗi business)
  → GlobalExceptionFilter catches → ApiResponse với error
  → Client
```

**Lưu ý**: `Result.cs` **KHÔNG tồn tại** trong project. Chỉ dùng Exception-based approach.
