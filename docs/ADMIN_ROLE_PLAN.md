# Kế hoạch thêm Role ADMIN — TechStoreAPI

> **Tài liệu bàn giao** · Ngày khảo sát: 2026-07-15 · Phạm vi: BE `TechStoreAPI` (branch `develop`) + FE `tech_void` (branch `CallApi`)
>
> Mục tiêu: thêm role **Admin** để theo dõi vận hành hệ thống.

---

## 1. TL;DR cho người bận

**Thêm Admin về mặt kỹ thuật chỉ tốn 1 dòng** (`enum Role`), **không cần migration**, JWT đã sẵn sàng.

**NHƯNG KHÔNG ĐƯỢC LÀM NGAY.** Hệ thống đang có **2 lỗ hổng cho phép bất kỳ ai tự phong mình làm Admin**. Thêm `Admin` vào enum trước khi bịt lỗ = tự mở cửa cho người lạ chiếm quyền quản trị.

Ngoài ra khảo sát phát hiện thêm **lỗ hổng thao túng giá** (khách tự gửi phí ship, có thể gửi số âm → đơn về 0đ) — không liên quan Admin nhưng **cần vá gấp**.

👉 **Bắt buộc làm [Giai đoạn 0](#gđ-0--bịt-lỗ-bảo-mật-bắt-buộc-làm-trước) trước tiên.**

**3 việc gấp nhất, theo thứ tự:**
| # | Lỗ hổng | Ai cũng làm được gì | Sửa ở đâu |
|---|---|---|---|
| 1 | Tự đăng ký role | Tạo tài khoản Staff/Admin | `AuthenService.cs:123` |
| 2 | `set-role` không auth | Phong Admin cho bất kỳ ai | `AuthController.cs:176` |
| 3 | Phí ship do client gửi | Mua hàng 0đ | `OrderService.cs:160` |

---

## 2. Hiện trạng — cái gì đã sẵn sàng

Tin tốt: nền tảng tốt hơn kỳ vọng.

| Hạng mục | Trạng thái | Chi tiết |
|---|---|---|
| **Role lưu DB** | ✅ Không cần migration | `ApplicationDbContext.cs:31-33` dùng `HasConversion<string>()`; cột `Users.Role` là `text` thuần, **không có CHECK constraint / PG enum** → thêm giá trị enum mới là chạy được ngay |
| **JWT phát role** | ✅ Không cần sửa | `JwtService.cs:38-44` đã nhét `new Claim(ClaimTypes.Role, user.Role.ToString())` → `[Authorize(Roles="Admin")]` hoạt động ngay khi enum có `Admin` |
| **JWT validate** | ✅ Không cần sửa | `config/JwtConfiguration.cs` — `RoleClaimType` để mặc định (`ClaimTypes.Role`), khớp đúng cái JwtService phát ra |
| **Mọi đường login** | ✅ Không cần sửa | 4 chỗ mint token (`VerifyOtpAsync`, `VerifyEmailLinkAsync`, `RefreshTokenAsync`, `GoogleLoginAsync`) đều đọc `user.Role` từ DB → role tự chảy vào token |
| **FE đọc role** | ✅ Gần xong | `FE/lib/data/services/auth_service.dart:34` đã decode `claims['role']` → `apiClient.userRole`. Chỉ cần thêm nhánh `== 'Admin'` |
| **Repository** | ✅ Đủ dùng | `IGenericRepository<T>` có sẵn `GetAllAsync()`. `OrderService.cs:16` đã có tiền lệ inject thẳng `ApplicationDbContext` → viết query `GroupBy` cho thống kê rất dễ |

---

## 3. 🔴 CHẶN ĐƯỜNG — 3 lỗ hổng phải vá

> Cả 2 lỗ đã được **kiểm chứng trực tiếp trong code**, không phải suy đoán.

### Lỗ 1 — Tự đăng ký làm bất cứ role nào

`POST /api/auth/register` **không có `[Authorize]`**, mà DTO lại nhận `Role` từ client:

```csharp
// TechStore.Domain/DTOs/Request/CreateUserRequest.cs:15
public Role Role { get; set; } = Role.Customer;   // ← chỉ là giá trị MẶC ĐỊNH, client ghi đè được

// TechStore.Service/Service/AuthenService.cs:123
Role = request.Role,                               // ← gán thẳng, KHÔNG kiểm tra
```

**Khai thác hôm nay:**
```bash
curl -X POST http://localhost:5173/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"hacker@x.com","password":"123456","fullName":"X","role":"Staff"}'
```
→ Có ngay tài khoản Staff. **Thêm `Admin` vào enum = câu lệnh trên đổi thành `"role":"Admin"` là xong.**

### Lỗ 2 — Đổi role người khác không cần đăng nhập

```csharp
// TechStoreAPI/Controllers/AuthController.cs:176-183
[HttpPut("set-role")]          // ← KHÔNG có [Authorize]
public async Task<IActionResult> SetRole([FromQuery] string email, [FromQuery] string role = "Staff")
```

Bên trong `AuthenService.cs:162` dùng `Enum.TryParse<Role>(role, ignoreCase: true, ...)` — **tự động chấp nhận mọi tên enum mới** (và cả số `"2"`).

**Khai thác:** `curl -X PUT "http://localhost:5173/api/auth/set-role?email=<email bất kỳ>&role=Admin"`

> Chính comment trong code đã cảnh báo: *"Nên bảo vệ bằng auth hoặc gỡ bỏ ở môi trường thật"* (`AuthController.cs:171-172`).

### Lỗ 3 — Khách tự quyết phí ship → thao túng giá đơn ⚠️ MỚI

> Nằm trong code **"cộng tiền"** vừa thêm (thay đổi chưa commit). Không liên quan role Admin nhưng **mức độ nghiêm trọng ngang** — ghi vào đây để không bị bỏ sót.

```csharp
// TechStore.Domain/DTOs/CheckoutRequest.cs:24 — client gửi lên
public decimal ShippingFee { get; set; } = 0;

// TechStore.Service/Service/OrderService.cs:158-160
decimal total = cartItems.Sum(c => c.Product!.Price * c.Quantity);
total += request.ShippingFee;      // ← cộng thẳng, KHÔNG kiểm tra, KHÔNG tính lại

// TechStore.Service/Service/OrderService.cs:178
ShippingFee = request.ShippingFee, // ← lưu nguyên số client gửi
```

`POST /api/order/checkout` không có auth (xem §4), nên **bất kỳ ai cũng đặt được đơn với `shippingFee` âm**:

```bash
curl -X POST http://localhost:5173/api/order/checkout \
  -H "Content-Type: application/json" \
  -d '{"userId":"<guid>","shippingAddress":"...","paymentMethod":"COD","shippingFee":-11500000}'
```
→ `total` về **0 hoặc âm**. Với VNPay, số tiền cổng thu chính là `total` này → **mua hàng gần như miễn phí**. Còn làm sai luôn báo cáo doanh thu + hoa hồng shipper (tính từ `ShippingFee`).

**Tin tốt — fix dễ vì hạ tầng đã có sẵn:**
- `ShippingService.CalculateShippingAsync()` **đã tồn tại**, tính phí server-side từ khoảng cách Goong + config (`Goong:BaseShippingFee`, `Goong:PerKmShippingFee`) — chính là thứ `POST /api/shipping/calculate` đang dùng.
- FE **đã gửi sẵn** `destinationLat` / `destinationLng` trong body checkout (`FE/lib/data/services/order_service.dart`), nhưng `CheckoutRequest` **không khai báo 2 field này** nên model binding âm thầm vứt đi.

**Cách sửa (khuyến nghị):**
1. Thêm `DestinationLat` / `DestinationLng` vào `CheckoutRequest` (FE gửi rồi, không phải sửa FE).
2. Inject `IShippingService` vào `OrderService` (ctor hiện có `ApplicationDbContext`, `IUnitOfWork`, `IVnpayService`, `IMapper`, `INotificationService`).
3. **Tính lại phí server-side** rồi dùng số đó cho cả `total` lẫn `Order.ShippingFee`; **bỏ hẳn `ShippingFee` khỏi `CheckoutRequest`** (hoặc chỉ dùng để đối chiếu, lệch quá ngưỡng thì từ chối).
4. Chốt chặn tối thiểu nếu chưa kịp làm đủ: `if (fee < 0 || fee > MaxAllowedFee) throw ...` — **không bao giờ để phí âm**.

> **Nguyên tắc:** mọi thứ ảnh hưởng tới **số tiền phải trả** đều phải tính ở server. Client chỉ được gửi *ý định* (địa chỉ/toạ độ), không được gửi *kết quả* (số tiền).

---

## 4. 🟠 Hiện trạng phân quyền toàn API

**Toàn hệ thống chỉ có ĐÚNG 2 dòng kiểm tra role thật sự:** `[Authorize(Roles="Staff")]` ở `CategoryController.cs:54` (PUT) và `:67` (DELETE).

| Controller | Phân quyền | Ghi chú |
|---|---|---|
| **OrdersController** | ❌ **Không có `[Authorize]` nào** | Kể cả `GET /api/Orders` (**lộ toàn bộ PII khách**) và `DELETE /api/Orders/{id}` |
| **CartsController** | ❌ Mở toang | Đọc/sửa giỏ của bất kỳ userId nào |
| **TrackingController** | ❌ Mở toang | `POST /api/tracking/location` nhận `ShipperId` **từ body** → giả mạo GPS bất kỳ shipper nào |
| **KnowledgeController** | ❌ Mở toang | CRUD knowledge base của AI — đúng ra là admin-only |
| **ChatController / ShippingController** | ❌ Mở toang | Gọi Gemini/Goong tốn quota, không giới hạn |
| **ProductsController** | ⚠️ Bị comment | `[Authorize(Roles="Staff")]` comment out ở dòng `78` (POST), `94` (PUT) |
| **CategoryController** | ⚠️ Một phần | POST comment out (`:44`); PUT/DELETE có bảo vệ ✅ |
| **DeviceController / NotificationsController** | ✅ `[Authorize]` | Chỉ check đăng nhập; đọc `ClaimTypes.NameIdentifier` — **đây là pattern mẫu nên theo** |
| **TrackingHub** (SignalR) | ❌ Không `[Authorize]` | |

**Nghiêm trọng nhất — nghiệp vụ Staff không kiểm tra gì:**
```csharp
// OrdersController.cs:252-261
[HttpPut("{id}/assign-shipper")]
public async Task<IActionResult> AssignShipper(Guid id, [FromQuery] Guid staffId)
```
Không `[Authorize]`, không đọc claims, **không kiểm tra `staffId` có phải user thật / có phải Staff không**. `confirm-delivery` còn tệ hơn: không nhận staffId, ai có GUID đơn cũng tự xác nhận "đã giao" được.

---

## 5. 📊 Admin cần gì mà BE chưa có

| Nhu cầu | Hiện trạng |
|---|---|
| Danh sách người dùng | ❌ **Không có `UsersController`**. Data layer sẵn sàng (`IUserRepositories` kế thừa `GetAllAsync()`), chỉ thiếu lớp expose |
| Thống kê doanh thu / đếm đơn | ❌ **Zero endpoint aggregate**. Admin phải `GET /api/Orders` tải **toàn bộ đơn từ trước tới nay** kèm `orderDetails` lồng nhau rồi tự tính client-side. Không filter ngày → payload phình vô hạn |
| Phân trang / lọc đơn | ❌ Không có query param nào |
| Hàng sắp hết | ❌ Phải tải hết products rồi tự lọc |
| Sửa riêng tồn kho | ❌ Chỉ có `PUT /api/Products/{id}` `[FromForm]` — phải gửi lại **toàn bộ** field + ảnh |
| Xoá sản phẩm | ❌ Không có endpoint DELETE |
| Danh sách shipper | ❌ Không → FE hiện phải biết GUID staff từ ngoài |

### Mỏ vàng chưa ai đụng (đúng thứ "theo dõi vận hành" cần)

Các bảng này **đầy dữ liệu vận hành nhưng không endpoint nào đọc**:

| Bảng | Dữ liệu | Dùng được cho |
|---|---|---|
| `LoginSession` | `Status` (Pending/Approved/Expired), `OtpCode`, `IsOtpSent`, `DeviceId/Name/Type`, `CreatedAt`, `ExpiredAt` | Nhật ký đăng nhập, tỉ lệ xác thực OTP thất bại, phát hiện brute-force |
| `UserDevice` | `DeviceId`, `DeviceName`, `DeviceType`, `FcmToken`, `CreatedAt/UpdatedAt` | Thiết bị đang hoạt động, phân bố nền tảng |
| `RefreshToken` | `IsRevoked`, `ExpiryDate`, `CreatedAt` | Phiên còn sống, phát hiện token bị thu hồi bất thường |
| `Message` | `SenderId`, `MessageText`, `SentAt` | ⚠️ **Bảng chết** — có DbSet nhưng không service/controller nào dùng |

### Bug phát hiện thêm (nên sửa luôn khi làm dashboard)

1. **`customerName` / `customerEmail` luôn RỖNG** trong `GET /api/Orders` và `GET /api/Orders/{id}`.
   Nguyên nhân: `MappingProfile.cs:26-28` map từ `src.User`, nhưng `OrderRepository.GetOrdersWithDetailsAsync()` / `GetOrderByIdWithDetailsAsync()` **quên `.Include(o => o.User)`** (chỉ Include `OrderDetails.ThenInclude(Product)`).
   → Dashboard admin sẽ hiện đơn không tên khách. Fix: thêm `.Include(o => o.User)`.
2. `Product.CreatedAt` có trên model nhưng **không có trong `ProductResponseDTO`** → không làm được "sản phẩm mới tuần này".
3. `Order.DeliveryProofImageUrl` và `Order.ShipperVehicle` có trên model nhưng **không expose ra DTO**.
4. `UserResponse.IsActive` **luôn hardcode `true`** (`AuthenService.cs:140`, `:183`) — model `User` không hề có field này. DTO đang "nói dối".
5. CORS mở toang: `SetIsOriginAllowed(host => true).AllowCredentials()` (`Program.cs:11-20`).
6. Token bị lộ qua deep link URL: `techstore://login-success?accessToken=...` (`AuthController.cs:96`).
7. `WeatherForecastController` — rác scaffold, nên xoá.

---

## 6. 🛠 Kế hoạch thực thi

### GĐ 0 — Bịt lỗ bảo mật (BẮT BUỘC, làm trước)
**Ước tính: ~30 phút. Nên làm NGAY kể cả chưa cần Admin.**

- [ ] **Bỏ `Role` khỏi `CreateUserRequest`** (`CreateUserRequest.cs:15`) — hoặc giữ DTO nhưng ép cứng trong service:
  ```csharp
  // AuthenService.cs:123 — SỬA THÀNH:
  Role = Role.Customer,   // đăng ký công khai LUÔN LÀ Customer; nâng quyền qua endpoint admin
  ```
- [ ] **Bảo vệ `set-role`** (`AuthController.cs:176`): thêm `[Authorize(Roles = "Admin")]`, hoặc **xoá hẳn** endpoint này và chuyển sang `UsersController` ở GĐ 3.
- [ ] **Seed admin đầu tiên trực tiếp trong DB** (vì set-role đã khoá):
  ```sql
  UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = '<email admin>';
  ```
  > Chưa có seed nào trong `OnModelCreating` — cân nhắc thêm để môi trường mới có sẵn admin.
- [ ] Kiểm tra lại: `curl` đăng ký với `"role":"Staff"` phải ra Customer.
- [ ] **Vá lỗ phí ship (Lỗ 3)** — xem chi tiết cách sửa ở §3:
  - [ ] Thêm `DestinationLat`/`DestinationLng` vào `CheckoutRequest` (FE đã gửi sẵn)
  - [ ] Inject `IShippingService` vào `OrderService`, **tính lại phí server-side**
  - [ ] Bỏ `ShippingFee` khỏi `CheckoutRequest` (hoặc chỉ dùng đối chiếu)
  - [ ] Tối thiểu: chặn `fee < 0`
  - [ ] Test: `curl` checkout với `"shippingFee":-11500000` phải bị từ chối / phí tính lại đúng

### GĐ 1 — Thêm role Admin
**Ước tính: ~15 phút**

- [ ] `TechStore.Domain/Enum/Role.cs` — **append cuối** để giữ ordinal cũ ổn định:
  ```csharp
  public enum Role
  {
      Customer,   // 0
      Staff,      // 1
      Admin       // 2  ← thêm
  }
  ```
  > **Không cần migration** (cột là `text`). Không cần sửa `JwtService`, không cần sửa `TokenValidationParameters`.
- [ ] Sửa message hardcode `AuthenService.cs:167`: *"Chỉ chấp nhận: Customer hoặc Staff"* → thêm Admin.
- [ ] Verify: login bằng tài khoản vừa seed → decode JWT phải thấy claim role = `Admin`.

### GĐ 2 — Phân quyền thật
**Ước tính: ~2-3 giờ**

- [ ] Khai báo policy (hiện `JwtConfiguration.cs:86` chỉ có `AddAuthorization()` trống):
  ```csharp
  services.AddAuthorization(options =>
  {
      options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
      options.AddPolicy("StaffOrAdmin", p => p.RequireRole("Staff", "Admin"));
  });
  ```
- [ ] Bật lại attribute đang comment: `ProductsController.cs:78, 94`; `CategoryController.cs:44` → đổi thành `[Authorize(Policy = "StaffOrAdmin")]`.
- [ ] **Bảo vệ `OrdersController`**: `GET /api/Orders` (all) → `AdminOnly` hoặc `StaffOrAdmin`; `GET /user/{userId}` → chỉ chính chủ hoặc Staff/Admin; `DELETE` → `AdminOnly`.
- [ ] `KnowledgeController` → `AdminOnly`.
- [ ] **`assign-shipper` / `confirm-delivery` / `tracking/location`: lấy id người gọi từ JWT claims** (`User.FindFirst(ClaimTypes.NameIdentifier)`) thay vì nhận từ query/body. Theo pattern có sẵn ở `DeviceController` / `NotificationsController`.
- [ ] Siết CORS về danh sách origin cụ thể.

> ⚠️ **Lưu ý FE:** siết auth ở đây **sẽ làm vỡ app hiện tại** nếu FE không gửi Bearer token cho các endpoint đó. `ApiClient._headers` (FE) đã tự gắn `Authorization: Bearer` khi có `authToken` → cần test lại toàn bộ luồng khách + staff sau khi siết.

### GĐ 3 — Endpoint cho Admin
**Ước tính: ~3-4 giờ**

- [ ] **`UsersController`** `[Authorize(Policy="AdminOnly")]`:
  - `GET /api/users` — list (dùng `_unitOfWork.Users.GetAllAsync()` sẵn có). **TUYỆT ĐỐI không trả `PasswordHash`** → tạo DTO riêng.
  - `GET /api/users/{id}`
  - `PUT /api/users/{id}/role` — thay cho `set-role` cũ.
  - `GET /api/users?role=Staff` — phục vụ dropdown chọn shipper.
- [ ] **`AdminController` + `AdminService`** (theo template `CategoryService`: inject `IUnitOfWork`, đăng ký 1 dòng `AddScoped` trong `config/DependencyInjection.cs`):
  - `GET /api/admin/stats?from=&to=` — doanh thu, số đơn theo status, VNPay vs COD, phí ship.
    > Dùng `GroupBy` qua `ApplicationDbContext` — **có tiền lệ**: `OrderService.cs:16` đã inject `_context` song song `_unitOfWork`. (Lý do: `IGenericRepository.FindAsync` gọi `.ToListAsync()` ngay → không có `IQueryable` để aggregate server-side.)
  - `GET /api/admin/sessions` — đọc `LoginSession` (nhật ký đăng nhập/OTP).
  - `GET /api/admin/devices` — đọc `UserDevice`.
- [ ] **Paging + filter cho `GET /api/Orders`**: `?page=&pageSize=&status=&from=&to=`.
- [ ] **Fix bug `.Include(o => o.User)`** trong `OrderRepository` để có tên khách.
- [ ] Bổ sung `CreatedAt` vào `ProductResponseDTO`; endpoint sửa riêng tồn kho.

### GĐ 4 — FE Admin
**Ước tính: ~4-6 giờ**

- [ ] `AppRoutes.enterApp` (`FE/lib/presentation/routing/app_routes.dart`): thêm nhánh `apiClient.userRole == 'Admin'` → `AdminShell`. **Bỏ hardcode `staffEmails`** (đang là `{'vuquang02062004@gmail.com'}`) vì giờ role JWT đã thật.
- [ ] `AdminShell` — bottom nav: **Tổng quan** (thống kê) · **Người dùng** · **Đơn hàng** · **Vận hành** (phiên đăng nhập/thiết bị).
- [ ] Service FE: `AdminService` gọi các endpoint GĐ 3.
- [ ] Dùng lại design system sẵn có: skeleton (`AppColors.skeletonBase/Highlight`), motion tokens, `LiquidGlassPanel`, dual theme (light VOID PAPER / dark VOID LUXE).
  > ⚠️ `AppColors` là **getter theo theme** → không dùng được trong `const` widget. Tab mới trong shell phải theo pattern key-theo-theme (xem `RootShell`/`StaffShell`) vì cây `const` không tự rebuild khi đổi theme.

---

## 7. Bảng tra nhanh — file cần đụng

| File | Việc |
|---|---|
| `TechStore.Domain/Enum/Role.cs` | Thêm `Admin` |
| `TechStore.Domain/DTOs/Request/CreateUserRequest.cs:15` | Bỏ `Role` |
| `TechStore.Service/Service/AuthenService.cs:123` | Ép `Role.Customer` |
| `TechStore.Domain/DTOs/CheckoutRequest.cs:24` | **Bỏ `ShippingFee`**, thêm `DestinationLat/Lng` |
| `TechStore.Service/Service/OrderService.cs:158-178` | **Tính lại phí ship server-side** (inject `IShippingService`) |
| `TechStore.Service/Service/AuthenService.cs:167` | Sửa message hardcode |
| `TechStoreAPI/Controllers/AuthController.cs:176` | Bảo vệ/xoá `set-role` |
| `TechStoreAPI/config/JwtConfiguration.cs:86` | Thêm policy |
| `TechStoreAPI/config/DependencyInjection.cs` | Đăng ký `IAdminService`, `IUserService` |
| `TechStoreAPI/Controllers/{Products,Category,Orders,Knowledge}Controller.cs` | Gắn `[Authorize]` |
| `TechStore.Repository/Repositories/OrderRepository.cs` | Thêm `.Include(o => o.User)` |
| `TechStoreAPI/Controllers/UsersController.cs` | **TẠO MỚI** |
| `TechStoreAPI/Controllers/AdminController.cs` | **TẠO MỚI** |
| `FE/lib/presentation/routing/app_routes.dart` | Nhánh Admin, bỏ `staffEmails` |
| `FE/lib/presentation/screens/admin/` | **TẠO MỚI** |

---

## 8. Checklist nghiệm thu

**Bảo mật (GĐ 0-2)**
- [ ] `POST /api/auth/register` với `"role":"Admin"` → tài khoản tạo ra vẫn là **Customer**
- [ ] `PUT /api/auth/set-role` không kèm token → **401**
- [ ] Checkout với `"shippingFee":-11500000` → **bị từ chối**, hoặc phí được tính lại đúng theo khoảng cách (tổng đơn KHÔNG giảm)
- [ ] Token Customer gọi `GET /api/Orders` → **403**
- [ ] Token Staff gọi `GET /api/users` → **403**
- [ ] Token Admin gọi `GET /api/users` → **200**, và **không có `passwordHash`** trong response
- [ ] `assign-shipper` với token Customer → **403**; staffId lấy từ token, không nhận từ query

**Chức năng**
- [ ] App khách + app staff vẫn chạy đủ luồng sau khi siết auth (login → mua → checkout → giao → tracking)
- [ ] `GET /api/Orders` trả `customerName` **không rỗng**
- [ ] Login tài khoản admin → vào thẳng `AdminShell`
- [ ] Thống kê khớp số liệu tính tay từ danh sách đơn

**Kỹ thuật**
- [ ] `dotnet build` — 0 error
- [ ] `flutter analyze` sạch + `flutter test` xanh
- [ ] Không cần migration cho GĐ 1 (xác nhận `dotnet ef migrations list` không đổi)

---

## 9. Ghi chú rủi ro

1. **Đổi role không làm mất hiệu lực token đang có.** Role chỉ nằm ở `Users.Role`; access token sống 60 phút (`Jwt:AccessTokenExpirationMinutes`). Hạ quyền một user → token cũ vẫn có quyền tối đa 60 phút. `RefreshTokenAsync` có đọc lại role từ DB nên refresh sẽ cập nhật.
2. **Thứ tự enum:** append `Admin` vào cuối. DB lưu text nên ordinal không ảnh hưởng, nhưng chèn giữa sẽ đổi số của `Staff` — vô hại hiện tại nhưng đừng tạo thói quen xấu.
3. **Siết auth = có thể vỡ FE.** Làm GĐ 2 xong phải test lại toàn bộ luồng trên cả 2 máy ảo (khách + staff).
4. **`GET /api/Orders` không phân trang** — dashboard admin gọi thẳng sẽ ngày càng chậm. Ưu tiên làm paging sớm.
5. **Secrets đang nằm trong `appsettings.json`** (mật khẩu Supabase, `GeminiApiKey`, Brevo). Nếu file này đã commit lên GitHub → **key đã lộ, cần revoke + chuyển sang User Secrets / biến môi trường**. Việc này nằm ngoài phạm vi Admin role nhưng nên xử lý cùng đợt.

---

*Tài liệu này dựa trên khảo sát trực tiếp mã nguồn. Mọi số dòng tham chiếu theo commit tại thời điểm khảo sát — hãy đối chiếu lại nếu code đã đổi.*
