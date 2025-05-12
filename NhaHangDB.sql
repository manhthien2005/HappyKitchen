Save New Duplicate & Edit Raw Udemy Course


USE RestaurantDB
GO
DROP TABLE IF EXISTS MenuItemAttributes;    
DROP TABLE IF EXISTS MenuItemRatings;    
DROP TABLE IF EXISTS OrderDetails;         
DROP TABLE IF EXISTS Orders;               
DROP TABLE IF EXISTS Reservations;         
DROP TABLE IF EXISTS QRCodes;             
DROP TABLE IF EXISTS MenuItems;          
DROP TABLE IF EXISTS Categories;         
DROP TABLE IF EXISTS TrustedDevices;      
DROP TABLE IF EXISTS Users;            
DROP TABLE IF EXISTS RolePermissions;     
DROP TABLE IF EXISTS Permissions;        
DROP TABLE IF EXISTS Roles;               
DROP TABLE IF EXISTS Tables;               
DROP TABLE IF EXISTS Areas;               


CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleKey NVARCHAR(50) NOT NULL UNIQUE,
    RoleName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255)
);

CREATE TABLE Permissions (
    PermissionID INT IDENTITY(1,1) PRIMARY KEY,
    PermissionKey NVARCHAR(50) NOT NULL UNIQUE, 
    PermissionName NVARCHAR(100) NOT NULL,      
    Description NVARCHAR(255) NULL
);

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


CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Address NVARCHAR(255) NULL,
    UserType TINYINT NOT NULL CHECK (UserType IN (0,1)),              -- 0 = Khách hàng, 1 = Nhân viên
    PasswordHash VARCHAR(255) NOT NULL, 
    Salary DECIMAL(18,2) NULL,
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)) DEFAULT 0,      -- 0 = Hoạt động, 1 = Bị khóa, 2 = Nghỉ việc
    RoleID INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE SET NULL
);


CREATE TABLE Areas (
    AreaID INT IDENTITY(1,1) PRIMARY KEY,
    AreaName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);


CREATE TABLE Tables (
    TableID INT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(50) NOT NULL,
    AreaID INT NULL,
    Capacity INT NOT NULL CHECK (Capacity > 0),
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)),                -- 0 = Trống, 1 = Đã đặt trước, 2 = Đang sử dụng
    FOREIGN KEY (AreaID) REFERENCES Areas(AreaID) ON DELETE SET NULL
);


CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(50) NOT NULL
);


CREATE TABLE MenuItems (
    MenuItemID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    MenuItemImage VARCHAR(255) NULL,
    CategoryID INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(255) NULL,
    Status TINYINT NOT NULL CHECK (Status IN (0,1)), -- 0 = Hết hàng, 1 = Còn hàng
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE CASCADE ON UPDATE CASCADE
);

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

CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NULL,
    EmployeeID INT NULL,
    TableID INT NOT NULL,
    OrderTime DATETIME DEFAULT GETDATE(),
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2,3)), -- 0 = Đã hủy, 1 = Chờ xác nhận, 2 = Hoàn thành
    PaymentMethod TINYINT NOT NULL CHECK (PaymentMethod IN (0,1,2)),
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE NO ACTION,
    FOREIGN KEY (EmployeeID) REFERENCES Users(UserID) ON DELETE NO ACTION,
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON DELETE NO ACTION
);

CREATE TABLE OrderDetails (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    MenuItemID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    Note NVARCHAR(200) NULL,
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE ON UPDATE CASCADE,
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE ON UPDATE CASCADE
);


CREATE TABLE Reservations (
    ReservationID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NULL, 
    CustomerName NVARCHAR(100) NOT NULL,
    CustomerPhone NVARCHAR(15) NOT NULL,
    TableID INT NOT NULL,
    Capacity INT NOT NULL,
    CreatedTime DATETIME DEFAULT GETDATE() NOT NULL,
    ReservationTime DATETIME NOT NULL, 
    Duration INT NOT NULL,
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)), -- 0 = Đã hủy, 1 = Xác nhận
    Notes NVARCHAR(255) NULL,
    OrderID INT NULL,
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE SET NULL,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE SET NULL,
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON DELETE CASCADE
);


CREATE TABLE MenuItemAttributes (
    AttributeID INT IDENTITY(1,1) PRIMARY KEY,
    MenuItemID INT NOT NULL,
    AttributeName NVARCHAR(100) NOT NULL, 
    AttributeValue NVARCHAR(255) NOT NULL, 
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE TrustedDevices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    DeviceToken NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TrustedDevices_Employees FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE QRCodes (
    QRCodeID INT IDENTITY(1,1) PRIMARY KEY,
    TableID INT NOT NULL,
    QRCodeImage VARCHAR(255) NULL,
    MenuUrl VARCHAR(255) NULL,    
    AccessCount INT NOT NULL DEFAULT 0, 
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), 
    Status TINYINT NOT NULL CHECK (Status IN (0, 1)) DEFAULT 0, -- 0 = Hoạt động, 1 = Ko hoạt động
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON DELETE CASCADE
);

GO
-- Vai trò
INSERT INTO Roles (RoleKey, RoleName, Description) VALUES
(N'MANAGER', N'Quản lý', N'Quản lý toàn bộ hoạt động nhà hàng'),
(N'WAITER', N'Nhân viên phục vụ', N'Phục vụ khách hàng và xử lý đơn hàng'),
(N'CHEF', N'Đầu bếp', N'Chuẩn bị món ăn theo đơn hàng'),
(N'ADMIN', N'Quản trị viên', N'Quản trị hệ thống'),
(N'CASHIER', N'Thu ngân', N'Xử lý đơn hàng và hóa đơn thanh toán');

-- Bảng quyền
INSERT INTO Permissions (PermissionKey, PermissionName, Description) VALUES
(N'MENU_MANAGE', N'Quản lý thực đơn', N'Quản lý toàn bộ món ăn trong thực đơn'),
(N'MENU_CATEGORY_MANAGE', N'Quản lý danh mục món ăn', N'Quản lý các nhóm danh mục trong thực đơn'),
(N'MENU_TAG_MANAGE', N'Quản lý thẻ thực đơn', N'Quản lý thẻ nhóm món ăn'),
(N'TABLE_BOOKING_MANAGE', N'Quản lý đặt bàn', N'Xem và xử lý thông tin đặt bàn'),
(N'TABLE_QR_MANAGE', N'Quản lý mã QR bàn', N'Tạo và quản lý mã QR cho bàn'),
(N'CUSTOMER_ACCOUNT_MANAGE', N'Quản lý tài khoản khách hàng', N'Xem và chỉnh sửa thông tin khách hàng'),
(N'STAFF_ACCOUNT_MANAGE', N'Quản lý tài khoản nhân viên', N'Quản lý hồ sơ nhân viên'),
(N'ROLE_PERMISSION_MANAGE', N'Quản lý vai trò và phân quyền', N'Tạo, phân quyền, chỉnh sửa vai trò'),
(N'VIEW_REPORT', N'Xem báo cáo', N'Xem các báo cáo thống kê và doanh thu'),
(N'TABLE_MANAGE', N'Quản lý bàn', N'Quản lý danh sách bàn, chỉnh sửa, xóa, thêm mới'),
(N'ORDER_MANAGE', N'Quản lý đơn hàng', N'Tạo, chỉnh sửa, xóa và xem thông tin đơn hàng (gồm thanh toán)');

-- MANAGER: đầy đủ quyền (trừ ROLE_PERMISSION_MANAGE view-only, QR có thể giữ lại tùy ý)
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(1, 1, 1,1,1,1), 
(1, 2, 1,1,1,1), 
(1, 3, 1,1,1,1),
(1, 4, 1,1,1,1), 
(1, 5, 1,1,1,1),
(1, 6, 1,1,1,1), 
(1, 7, 1,1,1,1),
(1, 8, 0,0,0,0), -- ROLE_PERMISSION_MANAGE
(1, 9, 1,0,0,0), -- chỉ xem báo cáo
(1,10, 1,1,1,1), -- TABLE_MANAGE
(1,11, 1,1,1,1); -- ORDER_MANAGE

-- WAITER: xem menu, tạo/sửa đơn hàng, xử lý đặt bàn
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(2, 1, 1,0,0,0), 
(2, 2, 1,0,0,0), 
(2, 3, 1,0,0,0),
(2, 4, 1,1,1,0), 
(2, 5, 1,0,0,0),
(2, 6, 1,0,0,0), 
(2, 7, 0,0,0,0),
(2, 8, 0,0,0,0), 
(2, 9, 0,0,0,0),
(2,10, 1,0,0,0),
(2,11, 1,1,1,0); -- đơn hàng: tạo, sửa nhưng không xóa

-- CHEF: chỉ xem menu và đơn hàng
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(3, 1, 1,0,0,0), (3, 2, 1,0,0,0), (3, 3, 1,0,0,0),
(3, 4, 0,0,0,0), (3, 5, 0,0,0,0),
(3, 6, 0,0,0,0), (3, 7, 0,0,0,0),
(3, 8, 0,0,0,0), (3, 9, 0,0,0,0),
(3,10, 0,0,0,0), (3,11, 1,0,0,0); -- chỉ xem đơn hàng

-- ADMIN: toàn quyền mọi thứ
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(4, 1, 1,1,1,1), (4, 2, 1,1,1,1), (4, 3, 1,1,1,1),
(4, 4, 1,1,1,1), (4, 5, 1,1,1,1),
(4, 6, 1,1,1,1), (4, 7, 1,1,1,1),
(4, 8, 1,1,1,1), (4, 9, 1,1,1,1),
(4,10, 1,1,1,1), (4,11, 1,1,1,1);

-- CASHIER: chủ yếu xem, toàn quyền với đơn hàng
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
(5, 1, 1,0,0,0), (5, 2, 1,0,0,0), (5, 3, 1,0,0,0),
(5, 4, 1,0,0,0), (5, 5, 0,0,0,0),
(5, 6, 1,0,0,0), (5, 7, 0,0,0,0),
(5, 8, 0,0,0,0), (5, 9, 1,0,0,0),
(5,10, 1,0,0,0), (5,11, 1,1,1,1);




DECLARE @PasswordHash VARCHAR(255) = '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8'; -- 'password' đã hash
DECLARE @i INT = 1;
DECLARE @EmployeeCount INT = 20;

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
DECLARE @CustomerCount INT = 100;

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

INSERT INTO Users (FullName, PhoneNumber, Email, Address, UserType, PasswordHash, Status,RoleID, CreatedAt)
    VALUES (N'KhoaLe','0000000000','socola200500@gmail.com','HCM',1,'$2a$11$B4Wmvzb2jMjrpShWAoXyOOWDkEagCKWk0FCFFK/eiXH7scR9bGw92',0,1,DATEADD(DAY, -(RAND() * 60), GETDATE()))
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

-- Thêm dữ liệu vào bảng MenuItems
INSERT INTO MenuItems (Name, MenuItemImage, CategoryID, Price, Description, Status)
VALUES 
    -- Món khai vị
    (N'Gỏi cuốn tôm thịt', '/images/menu/goi-cuon.jpg', 1, 45000, N'Gỏi cuốn tươi với tôm, thịt heo và rau thơm', 1),
    (N'Chả giò hải sản', '/images/menu/cha-gio.jpg', 1, 55000, N'Chả giò giòn với nhân hải sản thơm ngon', 1),
    (N'Salad trộn kiểu Thái', '/images/menu/salad-thai.jpg', 1, 65000, N'Salad rau củ trộn chua cay kiểu Thái', 1),
    (N'Súp hải sản', '/images/menu/sup-hai-san.jpg', 1, 75000, N'Súp hải sản đậm đà với tôm, mực và các loại hải sản', 1),
    
    -- Món chính
    (N'Cơm chiên hải sản', '/images/menu/com-chien-hai-san.jpg', 2, 85000, N'Cơm chiên với hải sản tươi ngon', 1),
    (N'Bò lúc lắc', '/images/menu/bo-luc-lac.jpg', 2, 120000, N'Thịt bò xào với ớt chuông và hành tây', 1),
    (N'Cá hồi nướng', '/images/menu/ca-hoi-nuong.jpg', 2, 150000, N'Cá hồi Na Uy nướng với sốt chanh dây', 1),
    (N'Gà nướng sả', '/images/menu/ga-nuong-sa.jpg', 2, 110000, N'Gà nướng với sả và gia vị đặc biệt', 1),
    (N'Lẩu Thái hải sản', '/images/menu/lau-thai.jpg', 2, 250000, N'Lẩu Thái chua cay với hải sản tươi sống', 1),
    (N'Bún chả Hà Nội', '/images/menu/bun-cha.jpg', 2, 95000, N'Bún chả truyền thống kiểu Hà Nội', 1),
    
    -- Món tráng miệng
    (N'Chè hạt sen', '/images/menu/che-hat-sen.jpg', 3, 35000, N'Chè hạt sen với nước cốt dừa', 1),
    (N'Bánh flan', '/images/menu/banh-flan.jpg', 3, 30000, N'Bánh flan mềm mịn với caramel', 1),
    (N'Trái cây thập cẩm', '/images/menu/trai-cay.jpg', 3, 5000, N'Đĩa trái cây tươi theo mùa', 1),
    
    -- Đồ uống
    (N'Nước ép cam', '/images/menu/nuoc-cam.jpg', 4, 35000, N'Nước ép cam tươi', 1),
    (N'Sinh tố bơ', '/images/menu/sinh-to-bo.jpg', 4, 40000, N'Sinh tố bơ đặc creamy', 1),
    (N'Trà đào', '/images/menu/tra-dao.jpg', 4, 35000, N'Trà đào với đào tươi', 1),
    (N'Cà phê đen', '/images/menu/ca-phe-den.jpg', 4, 30000, N'Cà phê đen đậm đà', 1),
    (N'Cà phê sữa', '/images/menu/ca-phe-sua.jpg', 4, 35000, N'Cà phê sữa đặc', 1),
    
    -- Món đặc biệt
    (N'Cua rang me', '/images/menu/cua-rang-me.jpg', 5, 250000, N'Cua biển rang với sốt me chua ngọt', 1),
    (N'Tôm hùm nướng phô mai', '/images/menu/tom-hum-nuong.jpg', 5, 450000, N'Tôm hùm nướng với phô mai béo ngậy', 1),
    
    -- Món chay
    (N'Đậu hũ sốt nấm', '/images/menu/dau-hu-nam.jpg', 6, 75000, N'Đậu hũ non sốt nấm thơm ngon', 1),
    (N'Canh rau củ', '/images/menu/canh-rau-cu.jpg', 6, 65000, N'Canh rau củ thanh đạm', 1),
    
    -- Món nướng
    (N'Sườn nướng BBQ', '/images/menu/suon-nuong.jpg', 7, 135000, N'Sườn heo nướng với sốt BBQ', 1),
    (N'Thịt xiên nướng', '/images/menu/thit-xien-nuong.jpg', 7, 95000, N'Thịt bò và heo xiên nướng', 1),
    
    -- Món hải sản
    (N'Mực xào sa tế', '/images/menu/muc-xao.jpg', 8, 120000, N'Mực tươi xào với sa tế cay', 1),
    (N'Tôm sú nướng muối ớt', '/images/menu/tom-nuong.jpg', 8, 150000, N'Tôm sú nướng với muối ớt', 1);
GO


-- Thêm dữ liệu vào bảng MenuItemRatings
DECLARE @i INT = 1;
DECLARE @RatingCount INT = 200;

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
 DECLARE @OrderCount INT = 300; -- Tạo 300 đơn hàng

 WHILE @i <= @OrderCount
 BEGIN
     DECLARE @CustomerID INT = NULL;
     DECLARE @EmployeeID INT = NULL;
     DECLARE @TableID INT = (SELECT TOP 1 TableID FROM Tables ORDER BY NEWID());
     DECLARE @PaymentMethod TINYINT = CASE WHEN RAND() < 0.5 THEN 0 ELSE 2 END; -- Ngẫu nhiên chọn 0 hoặc 2
     
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
     
     -- Thêm đơn hàng
     INSERT INTO Orders (CustomerID, EmployeeID, TableID, OrderTime, Status, PaymentMethod)
     VALUES (@CustomerID, @EmployeeID, @TableID, @OrderTime, @Status, @PaymentMethod);
     
     -- Lấy OrderID vừa thêm
     DECLARE @OrderID INT = SCOPE_IDENTITY();
     
     -- Thêm chi tiết đơn hàng (1-8 món ăn mỗi đơn)
     DECLARE @ItemCount INT = CAST((RAND() * 7) + 1 AS INT);
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


PRINT N'Đã tạo dữ liệu mẫu thành công!';


USE RestaurantDB
GO
CREATE PROCEDURE sp_UpdateTableStatusByIds
    @TableIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DECLARE @TableIdTable TABLE (TableID INT);
        INSERT INTO @TableIdTable (TableID)
        SELECT CAST(value AS INT)
        FROM STRING_SPLIT(@TableIds, ',');

        UPDATE t
        SET t.Status = 
            CASE 
                WHEN EXISTS (
                    SELECT 1
                    FROM Reservations r
                    WHERE r.TableID = t.TableID
                    AND r.Status = 1
                    AND GETDATE() >= r.ReservationTime
                    AND GETDATE() < DATEADD(MINUTE, r.Duration, r.ReservationTime)
                ) THEN 1
                WHEN EXISTS (
                    SELECT 1
                    FROM Orders o
                    WHERE o.TableID = t.TableID
                    AND o.Status IN (1, 2)
                    AND o.OrderTime >= DATEADD(HOUR, -4, GETDATE())
                ) THEN 2
                ELSE 0
            END
        FROM Tables t
        WHERE t.TableID IN (SELECT TableID FROM @TableIdTable);
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorLine INT = ERROR_LINE();
        DECLARE @ErrorNumber INT = ERROR_NUMBER();

        INSERT INTO ErrorLog (ErrorMessage, ErrorLine, ErrorNumber, CreatedAt)
        VALUES (@ErrorMessage, @ErrorLine, @ErrorNumber, GETDATE());
    END CATCH
END;
GO