CREATE DATABASE RestaurantDB
USE RestaurantDB
GO


DROP TABLE IF EXISTS MenuItemAttributes;
DROP TABLE IF EXISTS MenuItemRatings;
DROP TABLE IF EXISTS MenuItemLabels;
DROP TABLE IF EXISTS OrderDetails;
DROP TABLE IF EXISTS Orders;
DROP TABLE IF EXISTS Reservations;
DROP TABLE IF EXISTS Tables;
DROP TABLE IF EXISTS Areas;
DROP TABLE IF EXISTS TrustedDevices;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS RolePermissions;
DROP TABLE IF EXISTS Permissions;
DROP TABLE IF EXISTS Roles;
DROP TABLE IF EXISTS MenuItems;
DROP TABLE IF EXISTS Categories;

-- Tạo bảng Roles
CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleKey NVARCHAR(50) NOT NULL UNIQUE,
    RoleName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255)
);

-- Tạo bảng Permissions
CREATE TABLE Permissions (
    PermissionID INT IDENTITY(1,1) PRIMARY KEY,
    PermissionKey NVARCHAR(50) NOT NULL UNIQUE, 
    PermissionName NVARCHAR(100) NOT NULL,      
    Description NVARCHAR(255) NULL
);

-- Tạo bảng RolePermissions (phải tạo sau Roles và Permissions vì có khóa ngoại)
CREATE TABLE RolePermissions (
    RoleID INT NOT NULL,
    PermissionID INT NOT NULL,
    CanView BIT NOT NULL DEFAULT 0,
    CanAdd BIT NOT NULL DEFAULT 0,
    CanEdit BIT NOT NULL DEFAULT 0,
    CanDelete BIT NOT NULL DEFAULT 0,
    PRIMARY KEY (RoleID, PermissionID),
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE CASCADE,
    FOREIGN KEY (PermissionID) REFERENCES Permissions(PermissionID) ON DELETE CASCADE
);

-- Tạo bảng Users (phải tạo sau Roles vì có khóa ngoại RoleID)
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Address NVARCHAR(255) NULL,
    UserType TINYINT NOT NULL CHECK (UserType IN (0,1)), -- 0 = Khách hàng, 1 = Nhân viên
    PasswordHash VARCHAR(255) NOT NULL, 
    Salary DECIMAL(10,2) NULL, -- Chỉ áp dụng cho nhân viên
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)) DEFAULT 0, -- 0 = Hoạt động, 1 = Bị khóa, 2 = Nghỉ việc
    RoleID INT NULL, -- Gán role trực tiếp
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE SET NULL
);

-- Tạo bảng Areas
CREATE TABLE Areas (
    AreaID INT IDENTITY(1,1) PRIMARY KEY,
    AreaName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

-- Tạo bảng Tables (phải tạo sau Areas vì có khóa ngoại AreaID)
CREATE TABLE Tables (
    TableID INT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(50) NOT NULL,
    AreaID INT NULL,
    Capacity INT NOT NULL CHECK (Capacity > 0),
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)), -- 0 = Trống, 1 = Đã đặt trước, 2 = Đang sử dụng
    FOREIGN KEY (AreaID) REFERENCES Areas(AreaID) ON DELETE SET NULL
);

-- Tạo bảng Reservations (phải tạo sau Users và Tables vì có khóa ngoại)
CREATE TABLE Reservations (
    ReservationID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NULL, 
    CustomerName NVARCHAR(100) NOT NULL,
    CustomerPhone NVARCHAR(15) NOT NULL,
    TableID INT NOT NULL,
	Capacity INT NOT NULL,
    CreatedTime DATETIME DEFAULT GETDATE() NOT NULL, -- Thời gian tạo đặt chỗ
    ReservationTime DATETIME NOT NULL, -- Thời gian khách đặt chỗ thực tế
    Duration INT NOT NULL, --Thời lượng khách dùng bữa 
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)), -- 0 = Đã hủy, 1 = Đang chờ, 2 = Đã xác nhận
    Notes NVARCHAR(255) NULL,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE SET NULL,
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON DELETE CASCADE
);

-- Tạo bảng Categories
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(50) NOT NULL
);

-- Tạo bảng MenuItems (phải tạo sau Categories vì có khóa ngoại CategoryID)
CREATE TABLE MenuItems (
    MenuItemID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    MenuItemImage VARCHAR(255) NULL,
    CategoryID INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Description NVARCHAR(255) NULL,
    Status TINYINT NOT NULL CHECK (Status IN (0,1)), -- 0 = Hết hàng, 1 = Còn hàng
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE CASCADE ON UPDATE CASCADE
);

-- Tạo bảng MenuItemRatings (phải tạo sau MenuItems và Users vì có khóa ngoại)
CREATE TABLE MenuItemRatings (
    RatingID INT IDENTITY(1,1) PRIMARY KEY,
    MenuItemID INT NOT NULL,
    UserID INT NOT NULL,
    Rating TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5), -- Chỉ cho phép từ 1 đến 5 sao
    Comment NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- Tạo bảng Orders (phải tạo sau Users và Tables vì có khóa ngoại)
CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NULL,
    EmployeeID INT NULL,
    TableID INT NOT NULL,
    OrderTime DATETIME DEFAULT GETDATE(),
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2,3)), -- 0 = Đã hủy, 1 = Chờ xác nhận, 2 = Đang chuẩn bị, 3 = Hoàn thành
    PaymentMethod NVARCHAR(50) NOT NULL,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE NO ACTION,
    FOREIGN KEY (EmployeeID) REFERENCES Users(UserID) ON DELETE NO ACTION,
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON DELETE NO ACTION
);

-- Tạo bảng OrderDetails (phải tạo sau Orders và MenuItems vì có khóa ngoại)
CREATE TABLE OrderDetails (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    MenuItemID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    Note NVARCHAR(200) NULL,
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE ON UPDATE CASCADE,
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE ON UPDATE CASCADE
);

-- Tạo bảng MenuItemAttributes (phải tạo sau MenuItems vì có khóa ngoại)
CREATE TABLE MenuItemAttributes (
    AttributeID INT IDENTITY(1,1) PRIMARY KEY,
    MenuItemID INT NOT NULL,
    AttributeName NVARCHAR(100) NOT NULL, -- Tên thuộc tính (VD: "Spiciness", "Calories")
    AttributeValue NVARCHAR(255) NOT NULL, -- Giá trị thuộc tính (VD: "Medium", "350 kcal")
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE ON UPDATE CASCADE
);

-- Tạo bảng TrustedDevices (phải tạo sau Users vì có khóa ngoại)
CREATE TABLE TrustedDevices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    DeviceToken NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TrustedDevices_Employees FOREIGN KEY (UserId) REFERENCES Users(UserId)
);


GO
-- Thêm dữ liệu vào bảng Roles
INSERT INTO Roles (RoleKey, RoleName, Description) VALUES
(N'MANAGER', N'Quản lý', N'Quản lý toàn bộ hoạt động nhà hàng'),
(N'WAITER', N'Nhân viên phục vụ', N'Phục vụ khách hàng và xử lý đơn hàng'),
(N'CHEF', N'Đầu bếp', N'Chuẩn bị món ăn theo đơn hàng'),
(N'ADMIN', N'Quản trị viên', N'Quản trị viên hệ thống'),
(N'CASHIER', N'Thu ngân', N'Xử lý thanh toán và hóa đơn');

-- Thêm dữ liệu vào bảng Permissions
INSERT INTO Permissions (PermissionKey, PermissionName, Description) VALUES
(N'ORDER_MANAGE', N'Quản lý đơn hàng', N'Quyền xem, tạo, chỉnh sửa và xóa đơn hàng'),
(N'MENU_MANAGE', N'Quản lý thực đơn', N'Quyền quản lý toàn bộ món ăn trong thực đơn'),
(N'MENU_CATEGORY_MANAGE', N'Quản lý danh mục món ăn', N'Quản lý các nhóm danh mục trong thực đơn'),
(N'MENU_TAG_MANAGE', N'Quản lý thẻ thực đơn', N'Quản lý thẻ nhóm món ăn'),
(N'TABLE_BOOKING_MANAGE', N'Quản lý đặt bàn', N'Xem và xử lý thông tin đặt bàn'),
(N'TABLE_QR_MANAGE', N'Quản lý mã QR bàn', N'Tạo và quản lý mã QR cho bàn'),
(N'CUSTOMER_ACCOUNT_MANAGE', N'Quản lý tài khoản khách hàng', N'Xem và chỉnh sửa thông tin khách hàng'),
(N'STAFF_ACCOUNT_MANAGE', N'Quản lý tài khoản nhân viên', N'Quản lý hồ sơ nhân viên'),
(N'ROLE_PERMISSION_MANAGE', N'Quản lý vai trò và phân quyền', N'Tạo, phân quyền, chỉnh sửa vai trò'),
(N'VIEW_REPORT', N'Xem báo cáo', N'Xem các báo cáo thống kê và doanh thu'),
(N'ORDER_PREPARE', N'Chuẩn bị món ăn', N'Đánh dấu món ăn đã sẵn sàng'),
(N'PAYMENT_MANAGE', N'Thu ngân', N'Xử lý hóa đơn và thanh toán');


INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete)
SELECT 4, Permissions.PermissionID, 1, 1, 1, 1 FROM Permissions;


-- MANAGER - Quyền cao, trừ chuẩn bị món ăn
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(1, 1, 1, 1, 1, 1),  -- ORDER_MANAGE
(1, 2, 1, 1, 1, 1),  -- MENU_MANAGE
(1, 3, 1, 1, 1, 1),  -- MENU_CATEGORY_MANAGE
(1, 4, 1, 1, 1, 1),  -- MENU_TAG_MANAGE
(1, 5, 1, 1, 1, 1),  -- TABLE_BOOKING_MANAGE
(1, 6, 1, 1, 1, 1),  -- TABLE_QR_MANAGE
(1, 7, 1, 1, 1, 1),  -- CUSTOMER_ACCOUNT_MANAGE
(1, 8, 1, 1, 1, 1),  -- STAFF_ACCOUNT_MANAGE
(1, 9, 1, 1, 1, 1),  -- ROLE_PERMISSION_MANAGE
(1, 10, 1, 1, 1, 1), -- VIEW_REPORT
(1, 11, 0, 0, 0, 0), -- ORDER_PREPARE
(1, 12, 1, 1, 1, 1); -- PAYMENT_MANAGE

-- WAITER - Phục vụ khách, xử lý đơn và đặt bàn
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(2, 1, 1, 1, 1, 0),  -- ORDER_MANAGE
(2, 2, 0, 0, 0, 0),
(2, 3, 0, 0, 0, 0),
(2, 4, 0, 0, 0, 0),
(2, 5, 1, 1, 1, 0),  -- TABLE_BOOKING_MANAGE
(2, 6, 0, 0, 0, 0),
(2, 7, 0, 0, 0, 0),
(2, 8, 0, 0, 0, 0),
(2, 9, 0, 0, 0, 0),
(2, 10, 0, 0, 0, 0),
(2, 11, 0, 0, 0, 0),
(2, 12, 0, 0, 0, 0);

-- CHEF - Chỉ chuẩn bị món ăn
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(3, 1, 1, 0, 0, 0),  -- ORDER_MANAGE (để xem món cần nấu)
(3, 2, 0, 0, 0, 0),
(3, 3, 0, 0, 0, 0),
(3, 4, 0, 0, 0, 0),
(3, 5, 0, 0, 0, 0),
(3, 6, 0, 0, 0, 0),
(3, 7, 0, 0, 0, 0),
(3, 8, 0, 0, 0, 0),
(3, 9, 0, 0, 0, 0),
(3, 10, 0, 0, 0, 0),
(3, 11, 1, 1, 1, 0),  -- ORDER_PREPARE
(3, 12, 0, 0, 0, 0);

-- CASHIER - Chỉ xử lý thanh toán
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(5, 1, 1, 0, 0, 0),  -- ORDER_MANAGE (xem đơn để tính tiền)
(5, 2, 0, 0, 0, 0),
(5, 3, 0, 0, 0, 0),
(5, 4, 0, 0, 0, 0),
(5, 5, 0, 0, 0, 0),
(5, 6, 0, 0, 0, 0),
(5, 7, 0, 0, 0, 0),
(5, 8, 0, 0, 0, 0),
(5, 9, 0, 0, 0, 0),
(5, 10, 1, 0, 0, 0), -- VIEW_REPORT
(5, 11, 0, 0, 0, 0),
(5, 12, 1, 1, 1, 0); -- PAYMENT_MANAGE

DECLARE @PasswordHash VARCHAR(255) = '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8'; -- 'password' đã hash
DECLARE @i INT = 1;
DECLARE @EmployeeCount INT = 10;

WHILE @i <= @EmployeeCount
BEGIN
    INSERT INTO Users (FullName, PhoneNumber, Email, Address, UserType, PasswordHash, Salary, Status, RoleID, CreatedAt)
    VALUES (
        CASE 
            WHEN @i % 5 = 0 THEN N'Nguyễn Văn ' + CHAR(64 + @i)
            WHEN @i % 5 = 1 THEN N'Trần Thị ' + CHAR(64 + @i)
            WHEN @i % 5 = 2 THEN N'Lê Minh ' + CHAR(64 + @i)
            WHEN @i % 5 = 3 THEN N'Phạm Hoàng ' + CHAR(64 + @i)
            ELSE N'Hoàng Thị ' + CHAR(64 + @i)
        END,
        '17' + RIGHT('0' + CAST(@i AS VARCHAR(2)), 2) + '123456',
        'employee' + CAST(@i AS VARCHAR(5)) + '@happykitchen.com',
        CASE 
            WHEN @i % 3 = 0 THEN N'123 Đường Lê Lợi, Quận 1, TP.HCM'
            WHEN @i % 3 = 1 THEN N'456 Đường Nguyễn Huệ, Quận 3, TP.HCM'
            ELSE N'789 Đường Trần Hưng Đạo, Quận 5, TP.HCM'
        END,
        1, -- Nhân viên
        @PasswordHash,
        CASE 
            WHEN @i % 5 = 1 THEN 10000000 -- Quản lý
            WHEN @i % 5 = 2 THEN 7000000 -- Đầu bếp
            WHEN @i % 5 = 3 THEN 6000000 -- Thu ngân
            WHEN @i % 5 = 4 THEN 5000000 -- Nhân viên phục vụ
            ELSE 5000000 -- Nhân viên phục vụ
        END,
        0, -- Hoạt động
        CASE 
            WHEN @i = 1 THEN 4 -- Admin
            WHEN @i % 5 = 1 THEN 1 -- Manager
            WHEN @i % 5 = 2 THEN 3 -- Chef
            WHEN @i % 5 = 3 THEN 5 -- Cashier
            ELSE 2 -- Waiter
        END,
        DATEADD(DAY, -(@i * 7), GETDATE()) -- Ngày tạo cách nhau 1 tuần
    );
    SET @i = @i + 1;
END;

-- Thêm dữ liệu vào bảng Users (Khách hàng)
SET @i = 1;
DECLARE @CustomerCount INT = 10;

WHILE @i <= @CustomerCount
BEGIN
    DECLARE @CreatedDate DATETIME = DATEADD(DAY, -(RAND() * 60), GETDATE()); -- Ngẫu nhiên trong 60 ngày qua
    
    INSERT INTO Users (FullName, PhoneNumber, Email, Address, UserType, PasswordHash, Status, CreatedAt)
    VALUES (
        CASE 
            WHEN @i % 5 = 0 THEN N'Nguyễn Thị ' + CHAR(64 + (@i % 26))
            WHEN @i % 5 = 1 THEN N'Trần Văn ' + CHAR(64 + (@i % 26))
            WHEN @i % 5 = 2 THEN N'Lê Thị ' + CHAR(64 + (@i % 26))
            WHEN @i % 5 = 3 THEN N'Phạm Văn ' + CHAR(64 + (@i % 26))
            ELSE N'Hoàng Minh ' + CHAR(64 + (@i % 26))
        END,
        '01' + RIGHT('2' + CAST(@i AS VARCHAR(3)), 3) + '12345',
        'customer' + CAST(@i AS VARCHAR(5)) + '@gmail.com',
        CASE 
            WHEN @i % 4 = 0 THEN N'123 Đường Nguyễn Trãi, Quận 5, TP.HCM'
            WHEN @i % 4 = 1 THEN N'456 Đường Lý Thường Kiệt, Quận 10, TP.HCM'
            WHEN @i % 4 = 2 THEN N'789 Đường Cách Mạng Tháng 8, Quận 3, TP.HCM'
            ELSE N'101 Đường Võ Văn Tần, Quận 3, TP.HCM'
        END,
        0, -- Khách hàng
        @PasswordHash,
        0, -- Hoạt động
        @CreatedDate
    );
    SET @i = @i + 1;
END;
GO

-- Thêm dữ liệu vào bảng Areas
INSERT INTO Areas (AreaName, Description)
VALUES 
    (N'Khu vực trong nhà', N'Khu vực có điều hòa, phù hợp cho gia đình'),
    (N'Khu vực ngoài trời', N'Khu vực sân vườn, thoáng mát'),
    (N'Khu vực VIP', N'Khu vực riêng tư, dịch vụ cao cấp'),
    (N'Khu vực sự kiện', N'Khu vực dành cho tổ chức tiệc, sự kiện');
GO

-- Thêm dữ liệu vào bảng Tables
DECLARE @i INT = 1;
DECLARE @TableCount INT = 30;

WHILE @i <= @TableCount
BEGIN
    INSERT INTO Tables (TableName, AreaID, Capacity, Status)
    VALUES (
        N'Bàn ' + CAST(@i AS NVARCHAR(5)),
        CASE 
            WHEN @i <= 10 THEN 1 -- Khu vực trong nhà
            WHEN @i <= 20 THEN 2 -- Khu vực ngoài trời
            WHEN @i <= 25 THEN 3 -- Khu vực VIP
            ELSE 4 -- Khu vực sự kiện
        END,
        CASE 
            WHEN @i % 3 = 0 THEN 2 -- Bàn 2 người
            WHEN @i % 3 = 1 THEN 4 -- Bàn 4 người
            ELSE 8 -- Bàn 8 người
        END,
        CASE 
            WHEN @i % 5 = 0 THEN 1 -- Đã đặt trước
            WHEN @i % 7 = 0 THEN 2 -- Đang sử dụng
            ELSE 0 -- Trống
        END
    );
    SET @i = @i + 1;
END;
GO

-- Thêm dữ liệu vào bảng Categories
INSERT INTO Categories (CategoryName)
VALUES 
    (N'Món khai vị'),
    (N'Món chính'),
    (N'Món tráng miệng'),
    (N'Đồ uống'),
    (N'Món đặc biệt'),
    (N'Món chay'),
    (N'Món nướng'),
    (N'Món hải sản');
GO

-- Thêm món ăn vào MenuItems
INSERT INTO MenuItems (Name, MenuItemImage, CategoryID, Price, Description, Status) VALUES
-- Khai vị
(N'Gỏi ngó sen tôm thịt', 'goi-ngo-sen.png', 1, 89000, N'Món gỏi thanh mát với tôm tươi, thịt luộc và ngó sen giòn.', 1),
(N'Nộm bò bóp thấu', 'nom-bo.png' ,1, 95000, N'Món gỏi thịt bò mềm, hòa quyện với vị chua ngọt đặc trưng.', 1),
(N'Bò tái chanh', 'bo-tai-chanh.png', 1, 95000, N'Thịt bò tái kết hợp với chanh tươi, hành tây và rau thơm.', 1),
(N'Súp hải sản bào ngư', 'sup-hai-san-bao-ngu.png', 1, 129000, N'Súp hải sản cao cấp với bào ngư, tôm và nấm hương.', 1),
(N'Chả giò hải sản', 'cha-gio-hai-san.png', 1, 89000, N'Chả giò giòn rụm, nhân hải sản tươi ngon, chấm kèm nước mắm chua ngọt.', 1),
(N'Hàu nướng phô mai', 'hau-nuong-pho-mai.png', 1, 120000, N'Hàu tươi nướng phô mai béo ngậy, thơm lừng.', 1),

-- Món chính
(N'Bò lúc lắc sốt tiêu đen', 'bo-luc-lac.png', 2, 139000, N'Thịt bò mềm, sốt tiêu đen cay nhẹ, ăn kèm khoai tây chiên.', 1),
(N'Sườn heo nướng mật ong', 'suon-heo-nuong.png',2, 129000, N'Sườn heo nướng vàng ruộm, thơm lừng với sốt mật ong đặc biệt.', 1),
(N'Gà nướng muối ớt Tây Nguyên', 'ga-nuong-muoi-ot.png', 2, 159000, N'Gà nướng đậm vị muối ớt, thơm ngon hấp dẫn.', 1),
(N'Vịt quay Bắc Kinh', 'vit-quay-bac-kinh.png', 2, 229000, N'Vịt quay da giòn, ăn kèm bánh tráng và nước sốt đặc biệt.', 1),
(N'Tôm hùm nướng bơ tỏi', 'tom-hum-nuong-bo-toi.png', 2, 399000, N'Tôm hùm nướng với bơ tỏi, thơm lừng và hấp dẫn.', 1),
(N'Cua rang me', 'cua-rang-me.png', 2, 349000, N'Cua tươi ngon rang cùng nước sốt me chua ngọt hấp dẫn.', 1),
(N'Lẩu gà lá é', 'lau-ga-la-e.png', 2, 239000, N'Lẩu gà đặc sản Đà Lạt với lá é thơm lừng.', 1),

-- Thức uống
(N'Cà phê sữa đá','ca-phe-sua-da.png', 3, 45000, N'Cà phê Việt Nam pha phin, hòa quyện cùng sữa đặc.', 1),
(N'Sinh tố bơ', 'sinh-to-bo.png',3, 65000, N'Sinh tố bơ béo ngậy, giàu dinh dưỡng và tốt cho sức khỏe.', 1),
(N'Trà đào cam sả', 'tra-dao-cam-sa.png', 3, 55000, N'Trà đào thơm lừng kết hợp vị cam tươi mát và sả.', 1),
(N'Matcha latte', 'matcha-latte.png', 3, 65000, N'Matcha latte thơm béo với vị trà xanh Nhật Bản.', 1),
(N'Nước sâm bí đao', 'nuoc-sam-bi-dao.png', 3, 45000, N'Nước sâm nấu từ bí đao, mía lau và thảo mộc, thanh nhiệt và giải độc.', 1),
(N'Chanh dây mật ong', 'chanh-day-mat-ong.png', 3, 55000, N'Chanh dây chua ngọt kết hợp với mật ong thơm lừng, tốt cho sức khỏe.', 1),

-- Tráng miệng
(N'Chè khúc bạch','che-khuc-bach.png', 4, 49000, N'Chè thanh mát với khúc bạch mềm mịn, hạnh nhân giòn, vải ngọt.', 1),
(N'Tiramisu mềm mịn','tiramisu.png', 4, 65000, N'Bánh tiramisu béo thơm, hòa quyện vị cà phê và cacao.', 1),
(N'Kem dừa non sầu riêng', 'kem-dua-non.png', 4, 89000, N'Kem dừa non mát lạnh, kết hợp sầu riêng thơm béo.', 1),
(N'Chè hạt sen long nhãn', 'che-hat-sen.png', 4, 69000, N'Chè hạt sen long nhãn ngọt thanh, tốt cho sức khỏe.', 1),
(N'Bánh mochi nhân đậu đỏ', 'mochi-dau-do.png', 4, 75000, N'Bánh mochi Nhật Bản dẻo mềm, nhân đậu đỏ ngọt bùi.', 1);


-- Thuộc tính của Gỏi ngó sen tôm thịt (MenuItemID = 1)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(1, N'Thành phần', N'Ngó sen, tôm, thịt heo, rau thơm, đậu phộng, hành phi'),
(1, N'Khẩu phần', N'1 dĩa (2-3 người)'),
(1, N'Thời gian hoàn tất', N'15 phút'),
(1, N'Mô tả món', N'Món gỏi thanh mát với tôm tươi, thịt luộc và ngó sen giòn, trộn đều với nước mắm chua ngọt.'),
(1, N'Hướng dẫn sử dụng', N'Dùng lạnh để cảm nhận độ giòn của ngó sen và vị tươi của tôm.'),
(1, N'Mô tả chi tiết', N'Gỏi ngó sen tôm thịt là món khai vị đặc trưng của ẩm thực Việt Nam, kết hợp hài hòa giữa vị giòn của ngó sen, vị ngọt của tôm tươi và thịt heo luộc mềm, điểm xuyết bởi đậu phộng rang và hành phi thơm lừng.');

-- Thuộc tính của Nộm bò bóp thấu (MenuItemID = 2)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(2, N'Thành phần', N'Thịt bò, dưa chuột, cà rốt, hành tây, rau thơm, đậu phộng'),
(2, N'Khẩu phần', N'1 dĩa (2-3 người)'),
(2, N'Thời gian hoàn tất', N'20 phút'),
(2, N'Mô tả món', N'Món gỏi thịt bò mềm, hòa quyện với vị chua ngọt đặc trưng, ăn kèm bánh tráng.'),
(2, N'Mô tả chi tiết', N'Nộm bò bóp thấu mang đến sự tươi mát từ rau củ như dưa chuột, cà rốt và hành tây, kết hợp với thịt bò mềm được trộn đều trong nước mắm chua ngọt, tạo nên hương vị đậm đà.');

-- Thuộc tính của Bò tái chanh (MenuItemID = 3)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(3, N'Thành phần', N'Thịt bò, chanh, hành tây, rau thơm, đậu phộng'),
(3, N'Khẩu phần', N'1 dĩa (2-3 người)'),
(3, N'Thời gian hoàn tất', N'10 phút'),
(3, N'Mô tả món', N'Thịt bò tái kết hợp với chanh tươi, hành tây và rau thơm, tạo nên hương vị độc đáo.'),
(3, N'Mô tả chi tiết', N'Bò tái chanh là món ăn nhẹ nhàng nhưng đầy cuốn hút, với thịt bò tái được làm chín nhẹ bằng nước cốt chanh, kết hợp cùng rau thơm và đậu phộng rang, mang đến trải nghiệm ẩm thực tươi mới.');

-- Thuộc tính của Súp hải sản bào ngư (MenuItemID = 4)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(4, N'Thành phần', N'Bào ngư, tôm, mực, nấm hương, ngô ngọt, trứng'),
(4, N'Khẩu phần', N'1 bát (1 người)'),
(4, N'Thời gian hoàn tất', N'25 phút'),
(4, N'Mô tả món', N'Súp hải sản cao cấp với bào ngư, tôm và nấm hương, giàu dinh dưỡng và hương vị.'),
(4, N'Mô tả chi tiết', N'Súp hải sản bào ngư là món ăn bổ dưỡng với sự kết hợp của bào ngư quý hiếm, tôm mực tươi ngon và nấm hương thơm ngát, được nấu sệt với trứng và ngô ngọt, thích hợp để khai vị hoặc dùng trong bữa ăn nhẹ.');

-- Thuộc tính của Chả giò hải sản (MenuItemID = 5)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(5, N'Thành phần', N'Tôm, mực, cua, hành lá, tiêu, bột chiên giòn'),
(5, N'Khẩu phần', N'1 dĩa (6-8 cuốn)'),
(5, N'Thời gian hoàn tất', N'20 phút'),
(5, N'Mô tả món', N'Chả giò giòn rụm, nhân hải sản tươi ngon, chấm kèm nước mắm chua ngọt.'),
(5, N'Mô tả chi tiết', N'Chả giò hải sản được làm từ tôm, mực và cua tươi, cuốn trong lớp bánh tráng mỏng, chiên vàng giòn, mang đến hương vị đậm đà của biển cả, phù hợp cho mọi bữa tiệc.');

-- Thuộc tính của Hàu nướng phô mai (MenuItemID = 6)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(6, N'Thành phần', N'Hàu tươi, phô mai mozzarella, tỏi, bơ, rau mùi'),
(6, N'Khẩu phần', N'1 dĩa (4-6 con hàu)'),
(6, N'Thời gian hoàn tất', N'15 phút'),
(6, N'Mô tả món', N'Hàu tươi nướng với phô mai béo ngậy, thơm lừng, ăn kèm bánh mì.'),
(6, N'Mô tả chi tiết', N'Hàu nướng phô mai là sự kết hợp hoàn hảo giữa vị ngọt tự nhiên của hàu tươi và lớp phô mai mozzarella tan chảy, được nướng cùng bơ tỏi thơm lừng, tạo nên món khai vị sang trọng.');

-- Thuộc tính của Bò lúc lắc sốt tiêu đen (MenuItemID = 7)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(7, N'Thành phần', N'Thịt bò, tiêu đen, hành tây, ớt chuông, tỏi, nước tương'),
(7, N'Khẩu phần', N'1 dĩa (1-2 người)'),
(7, N'Thời gian hoàn tất', N'25 phút'),
(7, N'Mô tả món', N'Thịt bò mềm, sốt tiêu đen cay nhẹ, ăn kèm khoai tây chiên và salad.'),
(7, N'Mô tả chi tiết', N'Bò lúc lắc sốt tiêu đen là món chính hấp dẫn với thịt bò áp chảo mềm ngọt, phủ sốt tiêu đen cay nhẹ, kết hợp cùng hành tây và ớt chuông xào thơm, thường được phục vụ cùng khoai tây chiên giòn.');

-- Thuộc tính của Sườn heo nướng mật ong (MenuItemID = 8)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(8, N'Thành phần', N'Sườn heo, mật ong, tỏi, nước mắm, tiêu, dầu ăn'),
(8, N'Khẩu phần', N'1 dĩa (1-2 người)'),
(8, N'Thời gian hoàn tất', N'30 phút'),
(8, N'Mô tả món', N'Sườn heo được tẩm ướp với sốt mật ong, nướng đến khi vàng ruộm, thơm lừng, ăn kèm dưa chua.'),
(8, N'Hướng dẫn sử dụng', N'Dùng nóng để cảm nhận trọn vẹn hương vị, có thể chấm kèm sốt BBQ.'),
(8, N'Mô tả chi tiết', N'Sườn heo nướng mật ong mang hương vị ngọt ngào từ mật ong tự nhiên, hòa quyện với vị mặn nhẹ của nước mắm và thơm nồng của tỏi, nướng vàng óng, là lựa chọn lý tưởng cho bữa ăn gia đình.');

-- Thuộc tính của Gà nướng muối ớt Tây Nguyên (MenuItemID = 9)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(9, N'Thành phần', N'Gà, muối ớt Tây Nguyên, sả, tỏi, dầu ăn'),
(9, N'Khẩu phần', N'1 con (4-5 người)'),
(9, N'Thời gian hoàn tất', N'45 phút'),
(9, N'Mô tả món', N'Gà nướng đậm vị muối ớt, thơm lừng với sả và tỏi, ăn kèm rau sống.'),
(9, N'Mô tả chi tiết', N'Gà nướng muối ớt Tây Nguyên là món ăn đặc sản với lớp da giòn, thịt gà thấm đẫm muối ớt cay nồng, hòa quyện cùng hương thơm của sả và tỏi, phù hợp cho các bữa tiệc đông người.');

-- Thuộc tính của Vịt quay Bắc Kinh (MenuItemID = 10)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(10, N'Thành phần', N'Vịt, ngũ vị hương, mật ong, giấm, đường, bột mì'),
(10, N'Khẩu phần', N'1 con (4-5 người)'),
(10, N'Thời gian hoàn tất', N'60 phút'),
(10, N'Mô tả món', N'Vịt quay da giòn, ăn kèm bánh tráng, dưa leo, hành lá và nước sốt đặc biệt.'),
(10, N'Mô tả chi tiết', N'Vịt quay Bắc Kinh nổi tiếng với lớp da giòn tan, thịt mềm thơm mùi ngũ vị hương, được phục vụ cùng bánh tráng mỏng, dưa leo giòn và nước sốt đậm đà, mang đậm phong cách ẩm thực Trung Hoa.');

-- Thuộc tính của Tôm hùm nướng bơ tỏi (MenuItemID = 11)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(11, N'Thành phần', N'Tôm hùm, bơ, tỏi, chanh, rau mùi, tiêu'),
(11, N'Khẩu phần', N'1 con (1-2 người)'),
(11, N'Thời gian hoàn tất', N'20 phút'),
(11, N'Mô tả món', N'Tôm hùm nướng với bơ tỏi, thơm lừng và hấp dẫn, ăn kèm salad.'),
(11, N'Mô tả chi tiết', N'Tôm hùm nướng bơ tỏi là món hải sản cao cấp với thịt tôm hùm ngọt chắc, thấm đẫm hương vị bơ tỏi béo ngậy, thêm chút chanh tươi để cân bằng, tạo nên món ăn sang trọng và tinh tế.');

-- Thuộc tính của Cua rang me (MenuItemID = 12)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(12, N'Thành phần', N'Cua, me, đường, tỏi, ớt, hành lá'),
(12, N'Khẩu phần', N'1 dĩa (2-3 người)'),
(12, N'Thời gian hoàn tất', N'30 phút'),
(12, N'Mô tả món', N'Cua tươi ngon rang cùng nước sốt me chua ngọt hấp dẫn, ăn kèm cơm trắng.'),
(12, N'Mô tả chi tiết', N'Cua rang me mang đến hương vị đậm đà với thịt cua tươi ngọt, phủ lớp sốt me chua ngọt được chế biến từ me tươi, tỏi và ớt, là món ăn lý tưởng khi dùng cùng cơm nóng.');

-- Thuộc tính của Lẩu gà lá é (MenuItemID = 13)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(13, N'Thành phần', N'Gà, lá é, nấm, rau cải, bún tươi, gia vị lẩu'),
(13, N'Khẩu phần', N'1 nồi (3-4 người)'),
(13, N'Thời gian hoàn tất', N'40 phút'),
(13, N'Mô tả món', N'Lẩu gà đặc sản Đà Lạt với lá é thơm lừng, nước dùng đậm đà.'),
(13, N'Mô tả chi tiết', N'Lẩu gà lá é là món ăn ấm nóng với nước dùng gà ngọt thanh, điểm nhấn là hương thơm đặc trưng của lá é Đà Lạt, kết hợp cùng nấm và rau cải, thích hợp cho những ngày se lạnh.');

-- Thuộc tính của Cà phê sữa đá (MenuItemID = 14)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(14, N'Thành phần', N'Cà phê phin, sữa đặc, đá'),
(14, N'Khẩu phần', N'1 ly (1 người)'),
(14, N'Thời gian hoàn tất', N'5 phút'),
(14, N'Mô tả món', N'Cà phê Việt Nam pha phin, hòa quyện cùng sữa đặc, uống lạnh.'),
(14, N'Mô tả chi tiết', N'Cà phê sữa đá là thức uống truyền thống Việt Nam, được pha từ cà phê phin đậm đà, thêm sữa đặc béo ngọt và đá lạnh, mang đến cảm giác sảng khoái tức thì.');

-- Thuộc tính của Sinh tố bơ (MenuItemID = 15)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(15, N'Thành phần', N'Bơ, sữa tươi, đường, đá'),
(15, N'Khẩu phần', N'1 ly (1 người)'),
(15, N'Thời gian hoàn tất', N'5 phút'),
(15, N'Mô tả món', N'Sinh tố bơ béo ngậy, giàu dinh dưỡng và tốt cho sức khỏe.'),
(15, N'Mô tả chi tiết', N'Sinh tố bơ được xay nhuyễn từ bơ tươi, sữa tươi và chút đường, tạo nên thức uống mịn màng, béo nhẹ, là lựa chọn hoàn hảo cho những ai yêu thích đồ uống lành mạnh.');

-- Thuộc tính của Trà đào cam sả (MenuItemID = 16)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(16, N'Thành phần', N'Trà đen, đào, cam, sả, đường, đá'),
(16, N'Khẩu phần', N'1 ly (1 người)'),
(16, N'Thời gian hoàn tất', N'5 phút'),
(16, N'Mô tả món', N'Trà đào thơm lừng kết hợp vị cam tươi mát và sả, uống lạnh.'),
(16, N'Mô tả chi tiết', N'Trà đào cam sả là thức uống giải nhiệt với vị ngọt thanh của đào, chua nhẹ của cam và hương thơm đặc trưng của sả, hòa quyện trong trà đen đậm vị, thích hợp cho mọi thời điểm trong ngày.');

-- Thuộc tính của Matcha latte (MenuItemID = 17)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(17, N'Thành phần', N'Bột matcha, sữa tươi, đường, đá'),
(17, N'Khẩu phần', N'1 ly (1 người)'),
(17, N'Thời gian hoàn tất', N'5 phút'),
(17, N'Mô tả món', N'Matcha latte thơm béo với vị trà xanh Nhật Bản, có thể uống nóng hoặc lạnh.'),
(17, N'Mô tả chi tiết', N'Matcha latte được làm từ bột matcha Nhật Bản nguyên chất, kết hợp với sữa tươi béo ngậy và chút đường, mang đến hương vị trà xanh đậm đà, phù hợp cho cả ngày đông ấm áp hay hè mát lạnh.');

-- Thuộc tính của Nước sâm bí đao (MenuItemID = 18)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(18, N'Thành phần', N'Bí đao, mía lau, đường phèn, thảo mộc'),
(18, N'Khẩu phần', N'1 ly (1 người)'),
(18, N'Thời gian hoàn tất', N'10 phút'),
(18, N'Mô tả món', N'Nước sâm nấu từ bí đao, mía lau và thảo mộc, thanh nhiệt và giải độc.'),
(18, N'Mô tả chi tiết', N'Nước sâm bí đao là thức uống truyền thống với vị ngọt nhẹ từ mía lau và đường phèn, kết hợp bí đao thanh mát và thảo mộc, giúp giải nhiệt và hỗ trợ sức khỏe.');

-- Thuộc tính của Chanh dây mật ong (MenuItemID = 19)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(19, N'Thành phần', N'Chanh dây, mật ong, đường, đá'),
(19, N'Khẩu phần', N'1 ly (1 người)'),
(19, N'Thời gian hoàn tất', N'5 phút'),
(19, N'Mô tả món', N'Chanh dây chua ngọt kết hợp với mật ong thơm lừng, tốt cho sức khỏe.'),
(19, N'Mô tả chi tiết', N'Chanh dây mật ong mang vị chua ngọt đặc trưng của chanh dây tươi, hòa cùng mật ong nguyên chất, tạo nên thức uống vừa ngon miệng vừa giàu vitamin, lý tưởng cho ngày hè.');

-- Thuộc tính của Chè khúc bạch (MenuItemID = 20)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(20, N'Thành phần', N'Sữa tươi, gelatin, hạnh nhân, vải thiều, nhãn, đường phèn'),
(20, N'Khẩu phần', N'1 chén (1 người)'),
(20, N'Thời gian hoàn tất', N'30 phút'),
(20, N'Mô tả món', N'Món chè thanh mát với những viên khúc bạch mềm mịn, béo nhẹ, kết hợp cùng nước chè ngọt thanh và hạnh nhân rang giòn.'),
(20, N'Mô tả chi tiết', N'Chè khúc bạch là món tráng miệng tinh tế với khúc bạch làm từ sữa tươi và gelatin, mềm mịn như panna cotta, ăn cùng vải thiều, nhãn tươi và hạnh nhân giòn, tạo nên sự hòa quyện hoàn hảo.');

-- Thuộc tính của Tiramisu mềm mịn (MenuItemID = 21)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(21, N'Thành phần', N'Phô mai mascarpone, lòng đỏ trứng, đường, cà phê, rượu rum, bánh ladyfinger'),
(21, N'Khẩu phần', N'1 miếng (1 người)'),
(21, N'Thời gian hoàn tất', N'60 phút'),
(21, N'Mô tả món', N'Bánh tiramisu béo thơm, hòa quyện vị cà phê và cacao, mềm mịn tan trong miệng.'),
(21, N'Mô tả chi tiết', N'Tiramisu mềm mịn là món tráng miệng Ý kinh điển với lớp bánh ladyfinger thấm cà phê và rượu rum, phủ kem phô mai mascarpone béo ngậy, rắc cacao, mang đến hương vị ngọt ngào đầy quyến rũ.');

-- Thuộc tính của Kem dừa non sầu riêng (MenuItemID = 22)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(22, N'Thành phần', N'Dừa non, sầu riêng, sữa tươi, đường, kem whipping'),
(22, N'Khẩu phần', N'1 ly (1 người)'),
(22, N'Thời gian hoàn tất', N'15 phút'),
(22, N'Mô tả món', N'Kem dừa non mát lạnh, kết hợp sầu riêng thơm béo, ăn kèm dừa nạo.'),
(22, N'Mô tả chi tiết', N'Kem dừa non sầu riêng là sự kết hợp độc đáo giữa vị béo ngọt của dừa non, hương thơm nồng nàn của sầu riêng và kem whipping mềm mịn, thêm dừa nạo để tăng độ hấp dẫn.');

-- Thuộc tính của Chè hạt sen long nhãn (MenuItemID = 23)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(23, N'Thành phần', N'Hạt sen, long nhãn, đường phèn, táo đỏ'),
(23, N'Khẩu phần', N'1 chén (1 người)'),
(23, N'Thời gian hoàn tất', N'40 phút'),
(23, N'Mô tả món', N'Chè hạt sen long nhãn ngọt thanh, tốt cho sức khỏe, giúp an thần.'),
(23, N'Mô tả chi tiết', N'Chè hạt sen long nhãn là món tráng miệng bổ dưỡng với hạt sen bùi bùi, long nhãn ngọt thơm và chút táo đỏ, nấu cùng đường phèn, mang lại cảm giác thư giãn và tốt cho giấc ngủ.');

-- Thuộc tính của Bánh mochi nhân đậu đỏ (MenuItemID = 24)
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(24, N'Thành phần', N'Bột nếp, đậu đỏ, đường, nước'),
(24, N'Khẩu phần', N'1 dĩa (4-5 viên)'),
(24, N'Thời gian hoàn tất', N'30 phút'),
(24, N'Mô tả món', N'Bánh mochi Nhật Bản dẻo mềm, nhân đậu đỏ ngọt bùi, ăn kèm trà xanh.'),
(24, N'Mô tả chi tiết', N'Bánh mochi nhân đậu đỏ được làm từ bột nếp dẻo dai, bao bọc lớp nhân đậu đỏ ngọt bùi, là món tráng miệng truyền thống Nhật Bản, thường dùng cùng trà xanh để cân bằng vị ngọt.');

PRINT N'Đã thêm dữ liệu thuộc tính cho tất cả các món ăn thành công!';

INSERT INTO MenuItemRatings (MenuItemID, UserID, Rating, Comment, CreatedAt)
VALUES 
(4, 1, 5, N'Rất ngon, sẽ quay lại lần nữa!','2025-03-13'),
(4, 1, 4, N'Hương vị tuyệt vời nhưng hơi mặn một chút.','2025-03-13'),
(4, 1, 3, N'Bình thường, không có gì đặc biệt.','2025-03-13'),
(4, 1, 5, N'Món này xuất sắc, rất đáng thử!','2025-03-13'),
(4, 1, 2, N'Không hợp khẩu vị của mình lắm.','2025-03-13');

-- Thêm dữ liệu vào bảng MenuItemRatings
DECLARE @i INT = 1;
DECLARE @RatingCount INT = 20;

WHILE @i <= @RatingCount
BEGIN
    DECLARE @UserID INT = (SELECT TOP 1 UserID FROM Users WHERE UserType = 0 ORDER BY NEWID());
    DECLARE @MenuItemID INT = (SELECT TOP 1 MenuItemID FROM MenuItems ORDER BY NEWID());
    DECLARE @RatingDate DATETIME = DATEADD(DAY, -(RAND() * 60), GETDATE()); -- Ngẫu nhiên trong 60 ngày qua
    
    INSERT INTO MenuItemRatings (MenuItemID, UserID, Rating, Comment, CreatedAt)
    VALUES (
        @MenuItemID,
        @UserID,
        CAST((RAND() * 4) + 1 AS TINYINT), -- Rating từ 1-5
        CASE 
            WHEN @i % 5 = 0 THEN N'Món ăn rất ngon, tôi sẽ quay lại!'
            WHEN @i % 5 = 1 THEN N'Chất lượng tốt, giá cả hợp lý.'
            WHEN @i % 5 = 2 THEN N'Khá ngon nhưng giá hơi cao.'
            WHEN @i % 5 = 3 THEN N'Phục vụ nhanh, món ăn đúng vị.'
            ELSE N'Tôi thích cách trình bày món ăn này.'
        END,
        @RatingDate
    );
    SET @i = @i + 1;
END;
GO

-- Thêm dữ liệu vào bảng Orders và OrderDetails
DECLARE @CurrentDate DATETIME = GETDATE();
DECLARE @FirstDayLastMonth DATETIME = DATEADD(MONTH, -1, DATEADD(DAY, 1-DAY(@CurrentDate), @CurrentDate));
DECLARE @FirstDayCurrentMonth DATETIME = DATEADD(DAY, 1-DAY(@CurrentDate), @CurrentDate);

DECLARE @i INT = 1;
DECLARE @OrderCount INT = 30; -- Tạo 300 đơn hàng

WHILE @i <= @OrderCount
BEGIN
    DECLARE @CustomerID INT = NULL;
    DECLARE @EmployeeID INT = NULL;
    DECLARE @TableID INT = (SELECT TOP 1 TableID FROM Tables ORDER BY NEWID());
    
    -- 80% đơn hàng có thông tin khách hàng
    IF RAND() < 0.8
    BEGIN
        SET @CustomerID = (SELECT TOP 1 UserID FROM Users WHERE UserType = 0 ORDER BY NEWID());
    END
    
    -- Tất cả đơn hàng đều có nhân viên phục vụ
    SET @EmployeeID = (SELECT TOP 1 UserID FROM Users WHERE UserType = 1 ORDER BY NEWID());
    
    -- Tạo thời gian đặt hàng ngẫu nhiên trong tháng hiện tại và tháng trước
    DECLARE @OrderTime DATETIME;
    IF @i <= @OrderCount * 0.6 -- 60% đơn hàng trong tháng hiện tại
        SET @OrderTime = DATEADD(MINUTE, CAST(RAND() * 60 * 24 * 30 AS INT), @FirstDayCurrentMonth);
    ELSE -- 40% đơn hàng trong tháng trước
        SET @OrderTime = DATEADD(MINUTE, CAST(RAND() * 60 * 24 * 30 AS INT), @FirstDayLastMonth);
    
    -- Xác định trạng thái đơn hàng
    DECLARE @Status TINYINT;
    DECLARE @RandomStatus FLOAT = RAND();
    
    IF @RandomStatus < 0.05 -- 5% đơn hàng bị hủy
        SET @Status = 0;
    ELSE IF @RandomStatus < 0.1 -- 5% đơn hàng đang chờ xác nhận
        SET @Status = 1;
    ELSE IF @RandomStatus < 0.2 -- 10% đơn hàng đang chuẩn bị
        SET @Status = 2;
    ELSE -- 80% đơn hàng đã hoàn thành
        SET @Status = 3;
    
    -- Phương thức thanh toán
    DECLARE @PaymentMethod NVARCHAR(50);
    DECLARE @RandomPayment FLOAT = RAND();
    
    IF @RandomPayment < 0.4
        SET @PaymentMethod = N'Tiền mặt';
    ELSE IF @RandomPayment < 0.7
        SET @PaymentMethod = N'Thẻ tín dụng';
    ELSE IF @RandomPayment < 0.9
        SET @PaymentMethod = N'Ví điện tử';
    ELSE
        SET @PaymentMethod = N'Chuyển khoản ngân hàng';
    
    -- Thêm đơn hàng
    INSERT INTO Orders (CustomerID, EmployeeID, TableID, OrderTime, Status, PaymentMethod)
    VALUES (@CustomerID, @EmployeeID, @TableID, @OrderTime, @Status, @PaymentMethod);
    
    -- Lấy OrderID vừa thêm
    DECLARE @OrderID INT = SCOPE_IDENTITY();
    
    -- Thêm chi tiết đơn hàng (1-5 món ăn mỗi đơn)
    DECLARE @ItemCount INT = CAST((RAND() * 4) + 1 AS INT);
    DECLARE @j INT = 1;
    
    WHILE @j <= @ItemCount
    BEGIN
        DECLARE @MenuItemID INT = (SELECT TOP 1 MenuItemID FROM MenuItems ORDER BY NEWID());
        DECLARE @Quantity INT = CAST((RAND() * 3) + 1 AS INT); -- 1-4 món mỗi loại
        
        INSERT INTO OrderDetails (OrderID, MenuItemID, Quantity)
        VALUES (@OrderID, @MenuItemID, @Quantity);
        
        SET @j = @j + 1;
    END;
    
    SET @i = @i + 1;
END;
GO

-- Cập nhật TotalAmount cho bảng Orders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'TotalAmount')
BEGIN
    ALTER TABLE Orders ADD TotalAmount DECIMAL(10,2) DEFAULT 0;
END
GO

-- Cập nhật giá trị TotalAmount dựa trên OrderDetails
UPDATE o
SET o.TotalAmount = (
    SELECT SUM(od.Quantity * mi.Price)
    FROM OrderDetails od
    JOIN MenuItems mi ON od.MenuItemID = mi.MenuItemID
    WHERE od.OrderID = o.OrderID
)
FROM Orders o;
GO

PRINT N'Đã tạo dữ liệu mẫu thành công!';

