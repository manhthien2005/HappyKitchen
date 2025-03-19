CREATE DATABASE RestaurantDB;
USE RestaurantDB;
GO

CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE NULL,
    Address NVARCHAR(255) NULL,
    
    UserType TINYINT NOT NULL CHECK (UserType IN (0,1)) DEFAULT 0, -- 0 = Khách hàng, 1 = Nhân viên
    PasswordHash VARCHAR(255) NULL, -- Chỉ dùng cho nhân viên
    Salary DECIMAL(10,2) NULL, -- Chỉ áp dụng cho nhân viên
    
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)) DEFAULT 0 -- 0 = Hoạt động, 1 = Bị khóa, 2 = Nghỉ việc
);

CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

CREATE TABLE UserRoles (
    UserID INT NOT NULL,
    RoleID INT NOT NULL,
    PRIMARY KEY (UserID, RoleID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE CASCADE
);

CREATE TABLE Permissions (
    PermissionID INT IDENTITY(1,1) PRIMARY KEY,
    PermissionName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

CREATE TABLE RolePermissions (
    RoleID INT NOT NULL,
    PermissionID INT NOT NULL,
    PRIMARY KEY (RoleID, PermissionID),
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE CASCADE,
    FOREIGN KEY (PermissionID) REFERENCES Permissions(PermissionID) ON DELETE CASCADE
);

CREATE TABLE Areas (
    AreaID INT IDENTITY(1,1) PRIMARY KEY,
    AreaName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

CREATE TABLE Tables (
    TableID INT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(50) NOT NULL,
    AreaID INT NOT NULL,
    Capacity INT NOT NULL CHECK (Capacity > 0),
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)), -- 0 = Trống, 1 = Đã đặt trước, 2 = Đang sử dụng
    FOREIGN KEY (AreaID) REFERENCES Areas(AreaID) ON DELETE CASCADE
);


CREATE TABLE Reservations (
    ReservationID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NULL, 
    GuestName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15) NOT NULL,
    TableID INT NOT NULL,
    CreatedTime DATETIME DEFAULT GETDATE() NOT NULL, -- Thời gian tạo đặt chỗ
    ReservationTime DATETIME NOT NULL, -- Thời gian khách đặt chỗ thực tế
    Duration INT NOT NULL, --Thời lượng khách dùng bữa 
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)), -- 0 = Đã hủy, 1 = Đang chờ, 2 = Đã xác nhận
    Notes NVARCHAR(255) NULL,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON DELETE CASCADE
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
    Price DECIMAL(10,2) NOT NULL,
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
    TableID INT NOT NULL UNIQUE,
    OrderTime DATETIME DEFAULT GETDATE(),
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2,3)), -- 0 = Đã hủy, 1 = Chờ xác nhận, 2 = Đang chuẩn bị, 3 = Hoàn thành
    PaymentMethod NVARCHAR(50) NOT NULL,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE NO ACTION,
    FOREIGN KEY (EmployeeID) REFERENCES Users(UserID) ON DELETE NO ACTION,
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON UPDATE CASCADE
);

CREATE TABLE OrderDetails (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    MenuItemID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE ON UPDATE CASCADE,
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE ON UPDATE CASCADE
);


CREATE TABLE MenuItemAttributes (
    AttributeID INT IDENTITY(1,1) PRIMARY KEY,
    MenuItemID INT NOT NULL,
    AttributeName NVARCHAR(100) NOT NULL, -- Tên thuộc tính (VD: "Spiciness", "Calories")
    AttributeValue NVARCHAR(255) NOT NULL, -- Giá trị thuộc tính (VD: "Medium", "350 kcal")
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE TrustedDevices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    DeviceToken NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TrustedDevices_Employees FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- Thêm danh mục
INSERT INTO Categories (CategoryName) VALUES 
(N'Khai Vị'),
(N'Món Chính'),
(N'Thức Uống'),
(N'Tráng Miệng');

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


-- Thêm thuộc tính cho món ăn
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
-- Thuộc tính của Sườn heo nướng mật ong
(4, N'Thành phần', N'Sườn heo, mật ong, tỏi, nước mắm, tiêu, dầu ăn'),
(4, N'Khẩu phần', N'1 dĩa (1-2 người)'),
(4, N'Thời gian hoàn tất', N'30 phút'),
(4, N'Mô tả món', N'Sườn heo được tẩm ướp với sốt mật ong, nướng đến khi vàng ruộm, thơm lừng, ăn kèm dưa chua.'),
(4, N'Hướng dẫn sử dụng', N'Dùng nóng để cảm nhận trọn vẹn hương vị, có thể chấm kèm sốt BBQ.'),

-- Thuộc tính của Chè khúc bạch
(8, N'Thành phần', N'Sữa tươi, gelatin, hạnh nhân, vải thiều, nhãn, đường phèn'),
(8, N'Khẩu phần', N'1 chén (1 người)'),
(8, N'Mô tả món', N'Món chè thanh mát với những viên khúc bạch mềm mịn, béo nhẹ, kết hợp cùng nước chè ngọt thanh và hạnh nhân rang giòn.');

INSERT INTO Users ( FullName, PhoneNumber, Email, Address, UserType, PasswordHash, Salary, Status) 
VALUES 
(N'Nguyễn Văn A', '0987654321', 'nguyenvana@example.com', N'123 Đường ABC, TP.HCM', 1, 'hashed_password', 10000000, 0);


INSERT INTO MenuItemRatings (MenuItemID, UserID, Rating, Comment, CreatedAt)
VALUES 
(4, 1, 5, N'Rất ngon, sẽ quay lại lần nữa!','2025-03-13'),
(4, 1, 4, N'Hương vị tuyệt vời nhưng hơi mặn một chút.','2025-03-13'),
(4, 1, 3, N'Bình thường, không có gì đặc biệt.','2025-03-13'),
(4, 1, 5, N'Món này xuất sắc, rất đáng thử!','2025-03-13'),
(4, 1, 2, N'Không hợp khẩu vị của mình lắm.','2025-03-13');

-- Thêm dữ liệu mẫu cho bảng Areas
INSERT INTO Areas (AreaName, Description)
VALUES
(N'Khu VIP', N'Khu vực dành cho khách VIP, yên tĩnh và riêng tư.'),
(N'Khu Gia Đình', N'Khu vực phù hợp cho nhóm gia đình, không gian ấm cúng.'),
(N'Khu Ngoài Trời', N'Khu vực thoáng mát ngoài trời, gần gũi thiên nhiên.'),
(N'Khu Sảnh Chính', N'Khu vực trung tâm, dễ dàng phục vụ nhanh chóng.');

-- Thêm dữ liệu mẫu cho bảng Tables
INSERT INTO Tables (TableName, AreaID, Capacity, Status)
VALUES
-- Bàn thuộc Khu VIP
(N'Bàn VIP 1', 1, 4, 0),
(N'Bàn VIP 2', 1, 6, 1),
(N'Bàn VIP 3', 1, 8, 2),

-- Bàn thuộc Khu Gia Đình
(N'Bàn Gia Đình 1', 2, 6, 0),
(N'Bàn Gia Đình 2', 2, 8, 1),
(N'Bàn Gia Đình 3', 2, 10, 2),

-- Bàn thuộc Khu Ngoài Trời
(N'Bàn Ngoài Trời 1', 3, 4, 0),
(N'Bàn Ngoài Trời 2', 3, 6, 1),
(N'Bàn Ngoài Trời 3', 3, 8, 2),

-- Bàn thuộc Khu Sảnh Chính
(N'Bàn Sảnh 1', 4, 2, 0),
(N'Bàn Sảnh 2', 4, 4, 1),
(N'Bàn Sảnh 3', 4, 6, 2);


DROP TABLE IF EXISTS Reviews;
DROP TABLE IF EXISTS OrderDetails;
DROP TABLE IF EXISTS Orders;
DROP TABLE IF EXISTS MenuItemAttributes;
DROP TABLE IF EXISTS MenuItemRatings;
DROP TABLE IF EXISTS MenuItems;
DROP TABLE IF EXISTS Categories;
DROP TABLE IF EXISTS Reservations;
DROP TABLE IF EXISTS Tables;
DROP TABLE IF EXISTS TrustedDevices;
DROP TABLE IF EXISTS Areas;
DROP TABLE IF EXISTS RolePermissions;
DROP TABLE IF EXISTS Permissions;
DROP TABLE IF EXISTS UserRoles;
DROP TABLE IF EXISTS Roles;
DROP TABLE IF EXISTS Users;




