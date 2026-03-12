# Search / Sort / Paging — Implementation Guide

> **Mục đích**: Hướng dẫn AI (và developer) implement tính năng search, sort, paging cho feature mới một cách chuẩn, clean, không xung đột.  
> **Cập nhật**: 2026-02-14 · Đã áp dụng cho: PrenatalVisits, PrenatalTests, Languages  
> **Xem thêm**: `DEVELOPMENT_WORKFLOW_GUIDE.md` (conventions), `ARCHITECTURE_GUIDE.md` (response format)

---

## 1. TỔNG QUAN KIẾN TRÚC

### 1.1 Sơ đồ luồng

```
Client Request
  → Controller: [FromQuery] QueryOptions options
  → Service: verify ownership → gọi repo paged → map Entity → DTO
  → Repository: GetPagedAsync (predicate, include, searchBuilder, sortMap, defaultSort)
  → GenericRepository pipeline: soft-delete → predicate → search → COUNT → sort → include → pagination
  → PagedResult<TDto> trả về client
```

### 1.2 Thành phần cốt lõi (ĐÃ CÓ SẴN — KHÔNG tạo lại)

| File | Layer | Vai trò |
|------|-------|---------|
| `Application/DTOs/Common/QueryOptions.cs` | Application | Input DTO: Page, PageSize, Search, SearchIn, SortBy, SortDir, IncludeDeleted |
| `Application/DTOs/Common/PagedResult.cs` | Application | Output wrapper: Items, Page, PageSize, TotalItems, TotalPages, HasPrev/Next |
| `Application/DTOs/Common/QuerySpecMetadataDto.cs` | Application | Metadata cho FE: searchableFields, sortableFields, defaults |
| `Application/Common/Querying/SearchHelper.cs` | Application | Builds EF-translatable OR search predicates từ whitelist |
| `Application/Common/Querying/SortHelper.cs` | Application | Applies whitelist-based sorting với LambdaExpression (no boxing) |
| `Application/Common/Querying/QuerySpecRegistry.cs` | Application | Central registry metadata cho endpoint `GET /api/ref/query-specs` |
| `Infrastructure/Repositories/GenericRepository.cs` | Infrastructure | `GetPagedAsync` — pipeline hoàn chỉnh |

> ⚠️ **KHÔNG CHỈNH SỬA** các file trên khi implement feature mới. Chỉ dùng chúng.

---

## 2. IMPLEMENT PAGING CHO FEATURE MỚI — 6 BƯỚC

### Quy ước placeholder

Trong phần này, các placeholder dùng để thay thế cho entity/module cụ thể:

| Placeholder | Ý nghĩa | Ví dụ |
|-------------|---------|-------|
| `{Entity}` | Tên entity class | `WeightLog`, `MoodLog`, `ChatMessage`, `NutritionLog` |
| `{Module}` | Tên module/folder (số nhiều) | `WeightLogs`, `MoodLogs`, `ChatMessages` |
| `{EntityDto}` | Tên DTO class | `WeightLogDto`, `MoodLogDto` |
| `{ParentEntity}` | Entity cha (nếu scoped by FK) | `Pregnancy`, `Conversation` |
| `{parentId}` | FK param (camelCase) | `pregnancyId`, `conversationId` |
| `{route}` | Route URL segment | `weight-logs`, `mood-logs`, `messages` |
| `{permission}` | Permission key | `weight.read`, `mood.read`, `chat.read` |
| `{var}` | Variable prefix (lowercase) | `w`, `m`, `msg` |

Giả sử entity đã có: Entity, EF Config, Repository, Service, Controller (CRUD cơ bản).  
Cần thêm: search + sort + paging cho endpoint GET list.

---

### BƯỚC 1: Tạo QuerySpec (Application Layer)

**File**: `Application/Features/{Module}/{Entity}ListQuerySpec.cs`

```csharp
using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.{Module};

/// <summary>
/// Query specification for {Entity} entity listing.
/// Searchable: {string fields} | Sortable: {date/number/string fields}
/// </summary>
public static class {Entity}ListQuerySpec
{
    // ─── Search whitelist ──────────────────────────────────────
    // Key = tên field (lowercase, khớp với SearchIn query string)
    // Value = Expression trỏ đến string property của entity
    // CHỈ hỗ trợ string properties (vì search dùng .Contains())
    public static readonly Dictionary<string, Expression<Func<{Entity}, string?>>> SearchMap = new()
    {
        ["fieldname"] = {var} => {var}.FieldName,
        // Thêm field string nào muốn cho phép search
    };

    // Fields search mặc định khi client không truyền SearchIn
    public static readonly string[] DefaultSearchKeys = ["fieldname"];

    // ─── Sort whitelist ────────────────────────────────────────
    // Key = tên field (LOWERCASE — SortHelper convert về lowercase trước khi lookup)
    // Value = LambdaExpression (giữ nguyên type thật, tránh boxing)
    // Hỗ trợ MỌI property type: DateTime, DateOnly, string, decimal, int, bool,...
    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["datefield"]  = (Expression<Func<{Entity}, DateTime>>)({var} => {var}.DateField),
        ["numbefield"] = (Expression<Func<{Entity}, decimal>>)({var} => {var}.NumberField),
        ["createdat"]  = (Expression<Func<{Entity}, DateTime>>)({var} => {var}.CreatedAt)
    };

    // Sort mặc định khi client không truyền SortBy
    public static readonly LambdaExpression DefaultSort =
        (Expression<Func<{Entity}, DateTime>>)({var} => {var}.DateField);

    // ─── Metadata cho FE ───────────────────────────────────────
    // Tự sinh từ SearchMap/SortMap keys — không hardcode lại
    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "datefield",   // phải khớp key trong SortMap
        DefaultSortDir = "desc"
    };
}
```

**Quy tắc khi viết QuerySpec:**

| Quy tắc | Giải thích |
|---------|-----------|
| Sort key **phải lowercase** | `SortHelper` convert `SortBy` về lowercase trước khi lookup |
| Search key **phải lowercase** | `SearchHelper.ParseKeys()` convert về lowercase |
| Search chỉ hỗ trợ `string?` | Vì dùng `.Contains()`. Không thêm int/DateTime vào SearchMap |
| Sort hỗ trợ mọi type | Cast đúng type: `(Expression<Func<T, DateTime>>)`, `(Expression<Func<T, decimal>>)` |
| `DefaultSort` type phải khớp | Nếu defaultSort dùng `DateOnly` thì cast là `Expression<Func<T, DateOnly>>` |
| `Metadata.DefaultSortBy` khớp SortMap key | Phải là key tồn tại trong SortMap |

---

### BƯỚC 2: Đăng ký vào QuerySpecRegistry

**File**: `Application/Common/Querying/QuerySpecRegistry.cs`

Thêm 1 dòng mới vào dictionary:

```csharp
public static readonly IReadOnlyDictionary<string, QuerySpecMetadataDto> All =
    new Dictionary<string, QuerySpecMetadataDto>
    {
        // ... existing entries ...
        ["{module}"] = {Entity}ListQuerySpec.Metadata,  // ← THÊM DÒNG NÀY (camelCase key)
    };
```

> Key dùng **camelCase**, khớp với tên resource. VD: `"weightLogs"`, `"moodLogs"`, `"chatMessages"`.  
> FE nhận được qua `GET /api/ref/query-specs`.

---

### BƯỚC 3: Thêm method paged vào Repository Interface (Application Layer)

**File**: `Application/IRepositories/I{Entity}Repository.cs`

```csharp
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface I{Entity}Repository : IGenericRepository<{Entity}>
{
    // Method cũ (nếu có) — giữ nguyên cho internal use
    Task<List<{Entity}>> GetBy{ParentEntity}IdAsync(Guid {parentId}, CancellationToken ct = default);

    // Method mới — paged
    Task<PagedResult<{Entity}>> GetBy{ParentEntity}IdPagedAsync(
        Guid {parentId}, QueryOptions options, CancellationToken ct = default);
}
```

> Nếu không scope theo FK (e.g. admin list all), thay `GetBy{ParentEntity}IdPagedAsync` bằng `GetAllPagedAsync`.

---

### BƯỚC 4: Implement paged method trong Repository (Infrastructure Layer)

**File**: `Infrastructure/Repositories/{Entity}Repository.cs`

```csharp
using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Features.{Module};
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class {Entity}Repository : GenericRepository<{Entity}>, I{Entity}Repository
{
    public {Entity}Repository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<{Entity}>> GetBy{ParentEntity}IdPagedAsync(
        Guid {parentId}, QueryOptions options, CancellationToken ct = default)
    {
        return await GetPagedAsync(
            options,
            predicate: {var} => {var}.{ParentEntity}Id == {parentId},
            include: null,  // hoặc q => q.Include({var} => {var}.Navigation) nếu cần
            searchBuilder: SearchHelper.CreateSearchBuilder(
                {Entity}ListQuerySpec.SearchMap,
                {Entity}ListQuerySpec.DefaultSearchKeys,
                options),
            sortMap: {Entity}ListQuerySpec.SortMap,
            defaultSort: {Entity}ListQuerySpec.DefaultSort,
            cancellationToken: ct);
    }
}
```

**Pattern cố định khi gọi `GetPagedAsync` — COPY & THAY placeholder:**

```csharp
return await GetPagedAsync(
    options,                                         // QueryOptions từ controller
    predicate: {var} => {var}.{ParentEntity}Id == {parentId},  // Scope filter (FK)
    include: q => q.Include(...),                    // Navigation properties (hoặc null)
    searchBuilder: SearchHelper.CreateSearchBuilder(
        {Entity}ListQuerySpec.SearchMap,              // Lấy từ QuerySpec
        {Entity}ListQuerySpec.DefaultSearchKeys,
        options),
    sortMap: {Entity}ListQuerySpec.SortMap,           // Lấy từ QuerySpec
    defaultSort: {Entity}ListQuerySpec.DefaultSort,
    cancellationToken: ct);
```

---

### BƯỚC 5: Thêm paged method vào Service (Application Layer)

**File**: `Application/IServices/I{Entity}Service.cs` — thêm method:

```csharp
Task<PagedResult<{EntityDto}>> GetBy{ParentEntity}IdPagedAsync(
    Guid {parentId}, Guid userId, QueryOptions options, CancellationToken ct = default);
```

**File**: `Application/Services/{Entity}Service.cs` — implement:

```csharp
public async Task<PagedResult<{EntityDto}>> GetBy{ParentEntity}IdPagedAsync(
    Guid {parentId}, Guid userId, QueryOptions options, CancellationToken ct = default)
{
    // 1. Verify ownership (nếu cần)
    await Verify{ParentEntity}Ownership({parentId}, userId, ct);

    // 2. Gọi repo paged — KHÔNG gọi SearchHelper/SortHelper ở đây
    var pagedEntities = await _unitOfWork.{Module}
        .GetBy{ParentEntity}IdPagedAsync({parentId}, options, ct);

    // 3. Map entity → DTO
    var dtos = pagedEntities.Items.Select(MapToDto).ToList();

    // 4. Wrap lại PagedResult với DTO type
    return new PagedResult<{EntityDto}>(
        dtos, pagedEntities.Page, pagedEntities.PageSize, pagedEntities.TotalItems);
}
```

**Quy tắc Service:**

| ✅ ĐÚNG | ❌ SAI |
|---------|------|
| Service gọi `repo.GetBy{ParentEntity}IdPagedAsync(id, options)` | Service gọi `repo.GetPagedAsync(...)` trực tiếp với SearchHelper/SortHelper |
| Service chỉ verify ownership + map DTO | Service import `Microsoft.EntityFrameworkCore` |
| Service trả `PagedResult<{EntityDto}>` | Service trả `PagedResult<{Entity}>` |

> **Lý do**: Application layer KHÔNG reference EF Core. Search/sort/include logic thuộc Infrastructure.

---

### BƯỚC 6: Cập nhật Controller (API Layer)

**File**: `Api/Controllers/{Module}Controller.cs`

```csharp
using FPT.EXE201.Application.DTOs.Common; // ← thêm using cho QueryOptions

// Thay endpoint GET list cũ:

[HttpGet("api/{parent-route}/{parentId:guid}/{route}")]
[RequirePermission("{permission}")]
public async Task<IActionResult> GetBy{ParentEntity}(
    Guid {parentId},
    [FromQuery] QueryOptions options,  // ← ASP.NET tự bind từ query string
    CancellationToken ct)
{
    var result = await _{entity}Service
        .GetBy{ParentEntity}IdPagedAsync({parentId}, GetCurrentUserId(), options, ct);
    return Success(result);
}
```

**Query string tự động bind:**
```
GET /api/{parent-route}/{id}/{route}?Page=1&PageSize=10&Search=keyword&SortBy=fieldName&SortDir=desc
```

---

## 3. PIPELINE CHI TIẾT CỦA `GenericRepository.GetPagedAsync`

```
1. _dbSet.AsNoTracking()
2. if (!IncludeDeleted) → WHERE DeletedAt == null
3. if (predicate != null) → WHERE predicate (e.g., PregnancyId == X)
4. if (Search + searchBuilder) → WHERE (field1.Contains(term) OR field2.Contains(term))
5. COUNT(*) → totalItems
6. SortHelper.ApplySort → ORDER BY field ASC/DESC
7. if (include) → Include navigation properties
8. Skip + Take → OFFSET/LIMIT
9. ToListAsync → items
10. return new PagedResult<T>(items, page, pageSize, totalItems)
```

**Thứ tự quan trọng:**
- `COUNT` chạy **TRƯỚC** sort/include/pagination → đếm đúng số record sau filter
- `Include` chạy **SAU** sort → EF Core tối ưu query plan
- `Select(selector)` (overload DTO) chạy **SAU** include → projection trên server

---

## 4. API RESPONSE FORMAT

### Request
```
GET /api/{parent-route}/{id}/{route}?Page=2&PageSize=5&Search=keyword&SearchIn=notes&SortBy=dateField&SortDir=asc
```

### Response
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "statusCode": 200,
  "data": {
    "items": [
      { "id": "...", "field1": "value1", "field2": "value2" },
      { "id": "...", "field1": "value3", "field2": "value4" }
    ],
    "page": 2,
    "pageSize": 5,
    "totalItems": 23,
    "totalPages": 5,
    "hasPreviousPage": true,
    "hasNextPage": true
  },
  "errors": null,
  "timestamp": "2026-02-14T10:30:00Z"
}
```

### Query Parameters Reference

| Parameter | Type | Default | Mô tả |
|-----------|------|---------|-------|
| `Page` | int | 1 | Trang hiện tại (1-based). Min = 1 |
| `PageSize` | int | 20 | Số item mỗi trang. Min = 1, Max = 100 |
| `Search` | string? | null | Từ khóa tìm kiếm (LIKE '%...%') |
| `SearchIn` | string? | null | CSV field names để search. Nếu null → dùng DefaultSearchKeys |
| `SortBy` | string? | null | Field name để sort. Nếu null → dùng DefaultSort |
| `SortDir` | string | "desc" | "asc" hoặc "desc" |
| `IncludeDeleted` | bool | false | Bao gồm bản ghi đã soft-delete |

---

## 5. FE METADATA ENDPOINT

```
GET /api/ref/query-specs
```

Trả về toàn bộ search/sort capabilities cho tất cả paged endpoints:

```json
{
  "prenatalVisits": {
    "searchableFields": ["notes", "location"],
    "defaultSearchFields": ["notes", "location"],
    "sortableFields": ["visitdate", "location", "createdat"],
    "defaultSortBy": "visitdate",
    "defaultSortDir": "desc"
  },
  "prenatalTests": {
    "searchableFields": ["notes"],
    "defaultSearchFields": ["notes"],
    "sortableFields": ["testdate", "createdat"],
    "defaultSortBy": "testdate",
    "defaultSortDir": "desc"
  },
  "weightLogs": {
    "searchableFields": ["note"],
    "defaultSearchFields": ["note"],
    "sortableFields": ["loggedon", "weightkg", "createdat"],
    "defaultSortBy": "loggedon",
    "defaultSortDir": "desc"
  },
  "{module}": {
    "searchableFields": ["..."],
    "defaultSearchFields": ["..."],
    "sortableFields": ["..."],
    "defaultSortBy": "...",
    "defaultSortDir": "desc"
  }
}
```

FE gọi 1 lần → cache → render dynamic UI (checkbox search fields, dropdown sort).  
Mỗi feature mới đăng ký vào `QuerySpecRegistry` sẽ tự động xuất hiện ở đây.

---

## 6. EXISTING INFRASTRUCTURE CODE — REFERENCE

### 6.1 QueryOptions (KHÔNG SỬA)

```csharp
// Application/DTOs/Common/QueryOptions.cs
public class QueryOptions
{
    public int Page { get; set; }       // auto-clamp: min 1
    public int PageSize { get; set; }   // auto-clamp: min 1, max 100, default 20
    public string? Search { get; set; }
    public string? SearchIn { get; set; }  // CSV: "notes,location"
    public string? SortBy { get; set; }
    public string SortDir { get; set; } = "desc";
    public bool IncludeDeleted { get; set; } = false;
    public bool IsAscending => SortDir?.ToLower() == "asc";  // computed
}
```

### 6.2 PagedResult (KHÔNG SỬA)

```csharp
// Application/DTOs/Common/PagedResult.cs
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalItems { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);  // computed
    public bool HasPreviousPage => Page > 1;   // computed
    public bool HasNextPage => Page < TotalPages;  // computed

    public PagedResult() { Items = new List<T>(); }
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, long totalItems) { ... }
}
```

### 6.3 SearchHelper.CreateSearchBuilder (KHÔNG SỬA)

```csharp
// Tạo delegate cho GenericRepository.GetPagedAsync
SearchHelper.CreateSearchBuilder(
    searchMap,       // Dictionary<string, Expression<Func<T, string?>>>
    defaultKeys,     // string[]
    options          // QueryOptions (cần SearchIn)
)
// Returns: Func<IQueryable<T>, string, IQueryable<T>>
// Pipeline: parse SearchIn → filter whitelist → build OR predicate → apply WHERE
```

### 6.4 SortHelper.ApplySort (KHÔNG SỬA)

```csharp
// Apply sort vào query
SortHelper.ApplySort(
    query,           // IQueryable<T>
    options,         // QueryOptions (cần SortBy, SortDir)
    sortMap,         // Dictionary<string, LambdaExpression>
    defaultSort,     // LambdaExpression (fallback)
    defaultSortByCreatedAt: true  // fallback cuối cùng → CreatedAt
)
// Pipeline: lookup SortBy → fallback defaultSort → fallback CreatedAt → ApplyOrderDynamic
```

### 6.5 GenericRepository.GetPagedAsync — 2 Overloads (KHÔNG SỬA)

```csharp
// Overload 1: Trả entity
Task<PagedResult<T>> GetPagedAsync(
    QueryOptions options,
    Expression<Func<T, bool>>? predicate,
    Func<IQueryable<T>, IQueryable<T>>? include,
    Func<IQueryable<T>, string, IQueryable<T>>? searchBuilder,
    Dictionary<string, LambdaExpression>? sortMap,
    LambdaExpression? defaultSort,
    CancellationToken ct)

// Overload 2: Trả DTO (server-side projection)
Task<PagedResult<TDto>> GetPagedAsync<TDto>(
    QueryOptions options,
    Expression<Func<T, TDto>> selector,  // ← thêm projection
    ...) // same params
```

---

## 7. REAL EXAMPLES TỪ CODEBASE

### 7.1 PrenatalVisitListQuerySpec

```csharp
// Application/Features/PrenatalVisits/PrenatalVisitListQuerySpec.cs
public static class PrenatalVisitListQuerySpec
{
    public static readonly Dictionary<string, Expression<Func<PrenatalVisit, string?>>> SearchMap = new()
    {
        ["notes"] = v => v.Notes,
        ["location"] = v => v.Location
    };
    public static readonly string[] DefaultSearchKeys = ["notes", "location"];

    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["visitdate"] = (Expression<Func<PrenatalVisit, DateTime>>)(v => v.VisitDateTime),
        ["location"] = (Expression<Func<PrenatalVisit, string?>>)(v => v.Location),
        ["createdat"] = (Expression<Func<PrenatalVisit, DateTime>>)(v => v.CreatedAt)
    };
    public static readonly LambdaExpression DefaultSort =
        (Expression<Func<PrenatalVisit, DateTime>>)(v => v.VisitDateTime);

    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "visitdate",
        DefaultSortDir = "desc"
    };
}
```

### 7.2 PrenatalVisitRepository — Paged Method

```csharp
// Infrastructure/Repositories/PrenatalVisitRepository.cs
public async Task<PagedResult<PrenatalVisit>> GetByPregnancyIdPagedAsync(
    Guid pregnancyId, QueryOptions options, CancellationToken ct = default)
{
    return await GetPagedAsync(
        options,
        predicate: v => v.PregnancyId == pregnancyId,
        include: q => q.Include(v => v.Tests.Where(t => t.DeletedAt == null)),
        searchBuilder: SearchHelper.CreateSearchBuilder(
            PrenatalVisitListQuerySpec.SearchMap,
            PrenatalVisitListQuerySpec.DefaultSearchKeys,
            options),
        sortMap: PrenatalVisitListQuerySpec.SortMap,
        defaultSort: PrenatalVisitListQuerySpec.DefaultSort,
        cancellationToken: ct);
}
```

### 7.3 PrenatalVisitService — Paged Method

```csharp
// Application/Services/PrenatalVisitService.cs
public async Task<PagedResult<PrenatalVisitDto>> GetByPregnancyIdPagedAsync(
    Guid pregnancyId, Guid userId, QueryOptions options, CancellationToken ct = default)
{
    await VerifyPregnancyOwnership(pregnancyId, userId, ct);

    var pagedEntities = await _unitOfWork.PrenatalVisits
        .GetByPregnancyIdPagedAsync(pregnancyId, options, ct);

    var dtos = pagedEntities.Items.Select(MapToDto).ToList();
    return new PagedResult<PrenatalVisitDto>(
        dtos, pagedEntities.Page, pagedEntities.PageSize, pagedEntities.TotalItems);
}
```

### 7.4 PrenatalVisitsController — Endpoint

```csharp
// Api/Controllers/PrenatalVisitsController.cs
[HttpGet("api/pregnancies/{pregnancyId:guid}/visits")]
[RequirePermission("pregnancy.visit.read")]
public async Task<IActionResult> GetByPregnancy(
    Guid pregnancyId, [FromQuery] QueryOptions options, CancellationToken ct)
{
    var result = await _visitService
        .GetByPregnancyIdPagedAsync(pregnancyId, GetCurrentUserId(), options, ct);
    return Success(result);
}
```

---

## 8. CHECKLIST — Trước khi hoàn thành

- [ ] `Features/{Module}/{Entity}ListQuerySpec.cs` đã tạo đúng pattern
- [ ] Sort keys trong SortMap đều **lowercase**
- [ ] Search keys trong SearchMap đều **lowercase**
- [ ] `DefaultSortBy` trong Metadata khớp với key trong SortMap
- [ ] `QuerySpecRegistry.cs` đã thêm entry mới
- [ ] `IRepository` có method `Get...PagedAsync` trả `PagedResult<TEntity>`
- [ ] Repository implementation gọi `GetPagedAsync` với đúng params từ QuerySpec
- [ ] `IService` có method `Get...PagedAsync` trả `PagedResult<TDto>`
- [ ] Service **KHÔNG import** `Microsoft.EntityFrameworkCore` hoặc `SearchHelper`/`SortHelper`
- [ ] Service verify ownership rồi gọi repo → map DTO → wrap PagedResult
- [ ] Controller dùng `[FromQuery] QueryOptions options`
- [ ] Controller **KHÔNG** import `QuerySpecRegistry` hay helper classes
- [ ] `dotnet build` thành công, 0 errors, 0 warnings

---

## 9. ❌ SAI LẦM THƯỜNG GẶP

### ❌ 9.1 Viết search/sort config trong Repository

```csharp
// ❌ SAI — config là business logic, không thuộc Infrastructure
public class {Entity}Repository : GenericRepository<{Entity}>
{
    private static readonly Dictionary<string, LambdaExpression> SortMap = new() { ... };
}
```

```csharp
// ✅ ĐÚNG — config nằm ở Features/ trong Application layer
// Repository chỉ tham chiếu: {Entity}ListQuerySpec.SortMap
```

### ❌ 9.2 Import EF Core trong Service

```csharp
// ❌ SAI — vi phạm Clean Architecture
using Microsoft.EntityFrameworkCore;

public class {Entity}Service
{
    public async Task<PagedResult<{EntityDto}>> GetPagedAsync(...)
    {
        return await _unitOfWork.{Module}.GetPagedAsync(
            options,
            include: q => q.Include({var} => {var}.Navigation), // ← EF Core dependency!
            searchBuilder: SearchHelper.CreateSearchBuilder(...), // ← Infrastructure concern!
            ...);
    }
}
```

```csharp
// ✅ ĐÚNG — Service gọi custom repo method, repo handle EF details
public async Task<PagedResult<{EntityDto}>> GetPagedAsync(...)
{
    var paged = await _unitOfWork.{Module}.GetBy{ParentEntity}IdPagedAsync(id, options, ct);
    var dtos = paged.Items.Select(MapToDto).ToList();
    return new PagedResult<{EntityDto}>(dtos, paged.Page, paged.PageSize, paged.TotalItems);
}
```

### ❌ 9.3 Sort key KHÔNG lowercase

```csharp
// ❌ SAI — SortHelper convert SortBy về lowercase, sẽ không match
["visitDate"] = (Expression<Func<PrenatalVisit, DateTime>>)(v => v.VisitDateTime),

// ✅ ĐÚNG
["visitdate"] = (Expression<Func<PrenatalVisit, DateTime>>)(v => v.VisitDateTime),
```

### ❌ 9.4 Tạo 2 ParameterExpression riêng biệt cho fallback sort

```csharp
// ❌ SAI — 2 object khác nhau, EF Core crash runtime
sortExpression = Expression.Lambda(
    Expression.Property(Expression.Parameter(typeof(T), "e"), "CreatedAt"),
    Expression.Parameter(typeof(T), "e"));  // khác object!

// ✅ ĐÚNG — reuse 1 parameter
var param = Expression.Parameter(typeof(T), "e");
sortExpression = Expression.Lambda(Expression.Property(param, "CreatedAt"), param);
```

> Lỗi này đã fix trong `SortHelper.cs`. Ghi lại để tránh tái phạm khi sửa SortHelper.

### ❌ 9.5 Quên đăng ký vào QuerySpecRegistry

Nếu quên → `GET /api/ref/query-specs` không trả về metadata cho resource mới → FE không biết search/sort fields.

---

## 10. KHI NÀO KHÔNG CẦN PAGING

| Endpoint | Data volume | Cần paging? |
|----------|-------------|-------------|
| `GET /pregnancies` (per user) | 1–5 items | ❌ Không |
| `GET /pregnancies/{id}/conditions` | 0–10 items | ❌ Không |
| `GET /user-roles/me` | 1–3 items | ❌ Không |
| `GET /ref/pregnancy-conditions` | 10–50 items (admin-managed) | ❌ Không |
| `GET /ref/enums` | Static, hardcoded | ❌ Không |
| `GET /pregnancies/{id}/visits` | 10–40 items | ✅ Có |
| `GET /pregnancies/{id}/tests` | 5–30 items | ✅ Có |
| `GET /pregnancies/{id}/weight-logs` | 50–270 items (daily) | ✅ Có |
| `GET /pregnancies/{id}/mood-logs` | 50–270 items (daily) | ✅ Có |
| `GET /chat/conversations` | Unbounded | ✅ Có |
| `GET /chat/conversations/{id}/messages` | Unbounded | ✅ Có |

**Nguyên tắc**: Paging khi data > 20 items hoặc tăng theo thời gian (daily logs, messages, events).

---

## 11. FILE STRUCTURE TỔNG QUAN

```
src/FPT.EXE201.Application/
  Common/Querying/
    SearchHelper.cs              ← KHÔNG SỬA
    SortHelper.cs                ← KHÔNG SỬA
    QuerySpecRegistry.cs         ← CHỈ THÊM entry mới
  DTOs/Common/
    QueryOptions.cs              ← KHÔNG SỬA
    PagedResult.cs               ← KHÔNG SỬA
    QuerySpecMetadataDto.cs      ← KHÔNG SỬA
  Features/
    PrenatalVisits/
      PrenatalVisitListQuerySpec.cs  ← PATTERN MẪU (xem Section 7.1)
    PrenatalTests/
      PrenatalTestListQuerySpec.cs   ← PATTERN MẪU
    Languages/
      LanguageListQuerySpec.cs       ← PATTERN MẪU
    {Module}/
      {Entity}ListQuerySpec.cs       ← TẠO MỚI theo Bước 1
    ...

src/FPT.EXE201.Infrastructure/
  Repositories/
    GenericRepository.cs         ← KHÔNG SỬA (GetPagedAsync pipeline)
    PrenatalVisitRepository.cs   ← PATTERN MẪU (xem Section 7.2)
    {Entity}Repository.cs        ← THÊM paged method theo Bước 4

src/FPT.EXE201.Api/
  Controllers/
    RefDataController.cs         ← CÓ endpoint GET /api/ref/query-specs
```
