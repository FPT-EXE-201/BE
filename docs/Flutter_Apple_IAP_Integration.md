# Hướng dẫn Flutter — Apple In-App Purchase (Premium)

Tài liệu mô tả cách app **iOS** gọi backend sau khi mua gói Premium qua **StoreKit**, và cách test. Luồng **Android/Web** vẫn dùng PayOS (`POST /api/subscriptions/purchase`) — không đổi.

---

## 1. Chuẩn bị

### 1.1. Xcode / App Store Connect

- Bật capability **In-App Purchase** cho target iOS.
- **Bundle ID** khớp backend: `com.pregtap.app` (trong `appsettings.json`, `AppStore:BundleId`).
- **Product ID** (auto-renewable subscription) khớp backend:

| Gói     | Product ID |
|---------|------------|
| Tháng   | `com.pregtap.subscription.monthly` |
| 6 tháng | `com.pregtap.subscription.sixmonths` |
| Năm     | `com.pregtap.subscription.yearly` |

### 1.2. Package Flutter gợi ý

- [in_app_purchase](https://pub.dev/packages/in_app_purchase)
- Hoặc [in_app_purchase_storekit](https://pub.dev/packages/in_app_purchase_storekit) nếu tách riêng iOS.

Cần lấy **chuỗi JWS** của transaction sau khi mua (StoreKit 2: thường là `transaction.jwsRepresentation` hoặc tương đương từ plugin bạn dùng).

---

## 2. Phân nhánh theo nền tảng

```dart
import 'dart:io' show Platform;

bool get isIos => Platform.isIOS;

// iOS: StoreKit -> POST /api/subscriptions/apple/verify
// Android: giữ nguyên PayOS -> POST /api/subscriptions/purchase
```

Không mở PayOS / cổng thanh toán ngoài cho **mua Premium trong app iOS** — Apple sẽ reject nếu vi phạm guideline.

---

## 3. API Backend

### 3.1. Xác thực giao dịch sau khi mua

**Endpoint:** `POST /api/subscriptions/apple/verify`

**Headers:**

- `Authorization: Bearer <access_token>`
- `Content-Type: application/json`

**Body:**

```json
{
  "signedTransactionInfo": "<JWS từ StoreKit sau khi purchase thành công>"
}
```

**Quyền:** User cần permission `subscription.purchase` (theo JWT/role hiện tại của dự án).

**Response thành công:** Cùng format với các API subscription khác; `data` chứa trạng thái premium (`isPremium`, `plan`, `endDate`, `daysRemaining`, …).

**Lỗi thường gặp:**

| HTTP | Ý nghĩa |
|------|---------|
| 400  | JWS không hợp lệ / bundleId không khớp / productId không map được |
| 401  | Token hết hạn hoặc thiếu |
| 403  | Thiếu quyền `subscription.purchase` |

### 3.2. Restore purchases

1. StoreKit trả lại các transaction hợp lệ.
2. Với mỗi transaction (hoặc transaction đại diện), gửi **cùng** `POST /api/subscriptions/apple/verify` với `signedTransactionInfo` tương ứng.

Backend dùng `originalTransactionId` để **idempotent**: đã xử lý rồi thì trả trạng thái hiện tại, không tạo bản ghi trùng.

### 3.3. Trạng thái Premium

- `GET /api/subscriptions/status` — Bearer token, quyền `subscription.read` (theo cấu hình hiện tại).

Dùng sau khi verify thành công để cập nhật UI.

---

## 4. Luồng gợi ý (iOS)

1. User đăng nhập → lưu `access_token`.
2. Query product IDs từ StoreKit (khớp bảng Product ID ở trên).
3. User bấm mua → StoreKit hiển thị sheet thanh toán.
4. `purchaseStream` / completion → nhận transaction thành công.
5. Lấy `signedTransactionInfo` (JWS).
6. `POST /api/subscriptions/apple/verify` với body trên.
7. Nếu 200 → gọi `GET /status` nếu cần → cập nhật UI Premium.
8. Gọi `completePurchase` / finish transaction theo hướng dẫn plugin.

---

## 5. Test backend (Sandbox API, không thay StoreKit)

Chỉ khi server có `AppStore:Environment` = `Sandbox`.

**Endpoint:** `POST /api/subscriptions/apple/test-activate`

**Headers:** `Authorization: Bearer <token>`

**Body:**

```json
{
  "plan": "Monthly",
  "fakeTransactionId": "TEST_FLUTTER_001"
}
```

- `plan`: `Monthly` | `SixMonths` | `Yearly`
- `fakeTransactionId`: tùy chọn; để trống backend tự sinh.

**Mục đích:** Kiểm tra JWT, quyền, DB, role PREMIUM — **không** thay thế StoreKit trên TestFlight/App Store.

Trên **Production** endpoint này trả **400**.

---

## 6. Test Sandbox Apple (thiết bị thật / môi trường hỗ trợ)

1. Tạo **Sandbox Tester** trong App Store Connect.
2. Trên thiết bị: khi mua dùng account sandbox.
3. Build qua Xcode hoặc TestFlight.
4. Mua gói test → lấy JWS → gọi `/apple/verify` như mục 3.

---

## 7. App Store Server Notifications

Apple gọi server:

`POST /api/subscriptions/apple/notifications`

Body: `{ "signedPayload": "<JWS từ Apple>" }`

**Flutter không gọi endpoint này.** Cấu hình URL trong App Store Connect (Production + Sandbox), HTTPS.

---

## 8. Checklist trước khi nộp App Store

- [ ] iOS chỉ dùng IAP cho Premium trong app, không redirect PayOS cho mua trong app.
- [ ] Android vẫn dùng PayOS nếu đang dùng.
- [ ] Có nút **Restore Purchases** và gọi `/apple/verify` với transaction phục hồi.
- [ ] Sau verify: refresh user/profile nếu JWT embed quyền Premium.
- [ ] Xử lý lỗi mạng: retry hợp lý; finish transaction theo đúng lifecycle plugin.

---

## 9. Base URL

- Dev/local: theo `launchSettings` (ví dụ `http://localhost:5236`).
- Production: luôn **HTTPS**.

---

## 10. Postman

File: `BE/postman/Apple_IAP_Subscription.postman_collection.json`

Thứ tự: Register → Login → sandbox activate → Status → Cancel.

---

*Tài liệu phản ánh API Apple IAP song song PayOS. Đổi route/DTO thì cập nhật file này cùng PR.*
