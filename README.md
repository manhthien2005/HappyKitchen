# HappyKitchen

#New sql

## Khác biệt giữa hai phiên bản cơ sở dữ liệu

### 1. Gộp bảng `Customers` và `Employees` thành `Users`
- Trước đây:
  - `Customers(CustomerID, Name, Phone, Email, Address, CreatedAt)`
  - `Employees(EmployeeID, Name, Phone, Email, Role, PasswordHash, Salary, CreatedAt)`
- Hiện tại:
  - `Users(UserID, Name, Phone, Email, Address, UserType, PasswordHash, Salary, CreatedAt)`
  - **UserType**: `0` = Khách hàng, `1` = Nhân viên.
  
### 2. Thêm hệ thống phân quyền
- **Bảng mới:**
  - `Roles(RoleID, RoleName)`
  - `UserRoles(UserID, RoleID)`
  - `Permissions(PermissionID, PermissionName)`
  - `RolePermissions(RoleID, PermissionID)`
- Giúp phân quyền linh hoạt hơn cho nhân viên.

### 3. Thêm bảng `Areas` để quản lý khu vực nhà hàng
- **Bảng mới:** `Areas(AreaID, AreaName)`
- Bàn (`Tables`) được gán vào khu vực để dễ quản lý.

### 4. Cập nhật bảng `Tables`
- `TableNumber` → `TableName` (cho phép đặt tên thay vì chỉ đánh số).
- Thêm `AreaID` liên kết với `Areas`.

### 5. Cập nhật bảng `Reservations`
- `CustomerID` → `UserID` (tham chiếu `Users`).
- Thêm `Notes` để ghi chú chi tiết đặt chỗ.

### 6. Cập nhật bảng `Orders`
- `CustomerID`, `EmployeeID` → `UserID` (tham chiếu `Users`).
- `CustomerID` có thể `NULL` (hỗ trợ khách vãng lai).
- `TableID` không còn `UNIQUE` (hỗ trợ nhiều đơn trên một bàn).

### 7. Cập nhật bảng `MenuItems`
- `MenuItemImage` từ `NOT NULL` → `NULL` (cho phép nhập món ăn không có ảnh).

### 8. Giữ nguyên các bảng `Categories`, `OrderDetails`, `MenuItemAttributes`
- Điều chỉnh ràng buộc tham chiếu theo bảng `Users` mới.
