

USE RestaurantDB;
GO

-- Roles
CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

-- Permissions
CREATE TABLE Permissions (
    PermissionID INT IDENTITY(1,1) PRIMARY KEY,
    PermissionName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);

-- RolePermissions
CREATE TABLE RolePermissions (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID INT NOT NULL,
    PermissionID INT NOT NULL,
    CanView BIT NOT NULL DEFAULT 0,
    CanAdd BIT NOT NULL DEFAULT 0,
    CanEdit BIT NOT NULL DEFAULT 0,
    CanDelete BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE CASCADE,
    FOREIGN KEY (PermissionID) REFERENCES Permissions(PermissionID) ON DELETE CASCADE,
    CONSTRAINT UQ_Role_Permission UNIQUE (RoleID, PermissionID)
);
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL, 
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Address NVARCHAR(255) NULL,
    
    UserType TINYINT NOT NULL CHECK (UserType IN (0,1)), -- 0 = Khách hàng, 1 = Nhân viên
    PasswordHash VARCHAR(255) NOT NULL, 
    Salary DECIMAL(10,2) NULL, -- Chỉ áp dụng cho nhân viên
    
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)) DEFAULT 0, -- 0 = Hoạt động, 1 = Bị khóa, 2 = Nghỉ việc
    RoleID INT NULL, -- Gán role trực tiếp
    UserCreated UserCreated DATETIME NOT NULL DEFAULT GETDATE();
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
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)), -- 0 = Trống, 1 = Đã đặt trước, 2 = Đang sử dụng
    FOREIGN KEY (AreaID) REFERENCES Areas(AreaID) ON DELETE SET NULL
);


CREATE TABLE Reservations (
    ReservationID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NULL, 
    GuestName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15) NOT NULL,
    TableID INT NOT NULL,
    CreatedTime DATETIME DEFAULT GETDATE() NOT NULL, -- Thời gian tạo đặt chỗ
    ReservationTime DATETIME NOT NULL, -- Thời gian khách đặt chỗ thực tế
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2)), -- 0 = Đã hủy, 1 = Đang chờ, 2 = Đã xác nhận
    Notes NVARCHAR(255) NULL,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE SET NULL,
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON DELETE CASCADE
);

CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(50) NOT NULL
);

CREATE TABLE Labels (
    LabelID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL
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

CREATE TABLE MenuItemLabels (
    MenuItemID INT NOT NULL,
    LabelID INT NOT NULL,
    PRIMARY KEY (MenuItemID, LabelID),
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE,
    FOREIGN KEY (LabelID) REFERENCES Labels(LabelID) ON DELETE CASCADE
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
    Status TINYINT NOT NULL CHECK (Status IN (0,1,2,3)), -- 0 = Đã hủy, 1 = Chờ xác nhận, 2 = Đang chuẩn bị, 3 = Hoàn thành
    PaymentMethod NVARCHAR(50) NOT NULL,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE NO ACTION,
    FOREIGN KEY (EmployeeID) REFERENCES Users(UserID) ON DELETE NO ACTION,
    FOREIGN KEY (TableID) REFERENCES Tables(TableID) ON DELETE NO ACTION
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

USE RestaurantDB;
GO
-- Thêm dữ liệu vào bảng Roles
INSERT INTO Roles (RoleName, Description) VALUES
(N'Quản lý', N'Quản lý toàn bộ hoạt động nhà hàng'),
(N'Nhân viên phục vụ', N'Phục vụ khách hàng và xử lý đơn hàng'),
(N'Đầu bếp', N'Chuẩn bị món ăn theo đơn hàng');
GO
-- Thêm dữ liệu vào bảng Permissions
INSERT INTO Permissions (PermissionName, Description) VALUES
(N'Quản lý đơn hàng', N'Quyền liên quan đến đơn hàng (xem, thêm, sửa, xóa)'),
(N'Quản lý thực đơn', N'Quyền quản lý món ăn trong thực đơn'),
(N'Quản lý đặt bàn', N'Quyền quản lý bàn và đặt bàn'),
(N'Xem báo cáo', N'Quyền xem báo cáo doanh thu và thống kê'),
(N'Chuẩn bị món ăn', N'Quyền đánh dấu món ăn đã được chuẩn bị');

GO
-- Thêm dữ liệu vào bảng RolePermissions
INSERT INTO RolePermissions (RoleID, PermissionID, CanView, CanAdd, CanEdit, CanDelete) VALUES
-- Quản lý (RoleID = 1): Có tất cả quyền
(1, 1, 1, 1, 1, 1), -- Quản lý đơn hàng
(1, 2, 1, 1, 1, 1), -- Quản lý thực đơn
(1, 3, 1, 1, 1, 1), -- Quản lý đặt bàn
(1, 4, 1, 0, 0, 0), -- Xem báo cáo
(1, 5, 1, 0, 1, 0), -- Chuẩn bị món ăn
-- Nhân viên phục vụ (RoleID = 2): Quyền liên quan đến đơn hàng và đặt bàn
(2, 1, 1, 1, 1, 0), -- Quản lý đơn hàng (không xóa)
(2, 3, 1, 1, 1, 0), -- Quản lý đặt bàn (không xóa)
-- Đầu bếp (RoleID = 3): Chỉ chuẩn bị món ăn
(3, 5, 1, 0, 1, 0); -- Chuẩn bị món ăn

GO
-- Thêm dữ liệu vào bảng Users (10 khách hàng, 10 nhân viên)
INSERT INTO Users (Username, FullName, PhoneNumber, Email, Address, UserType, PasswordHash, Salary, Status, RoleID) VALUES
-- Khách hàng (UserType = 0)
('nguyenvan_a', N'Nguyễn Văn An', '0901234561', 'an.nguyen@gmail.com', N'123 Đường Láng, Đống Đa, Hà Nội', 0, 'hashed_password', NULL, 0, NULL),
('tranthi_b', N'Trần Thị Bình', '0901234562', 'binh.tran@gmail.com', N'45 Nguyễn Huệ, Quận 1, TP.HCM', 0, 'hashed_password', NULL, 0, NULL),
('phamvan_c', N'Phạm Văn Cường', '0901234563', 'cuong.pham@gmail.com', N'67 Lê Lợi, Hải Châu, Đà Nẵng', 0, 'hashed_password', NULL, 0, NULL),
('lethi_d', N'Lê Thị Duyên', '0901234564', 'duyen.le@gmail.com', N'89 Trần Phú, Nha Trang, Khánh Hòa', 0, 'hashed_password', NULL, 0, NULL),
('hoangvan_e', N'Hoàng Văn Em', '0901234565', 'em.hoang@gmail.com', N'12 Phạm Ngũ Lão, TP. Huế', 0, 'hashed_password', NULL, 0, NULL),
('vuthi_f', N'Vũ Thị Phượng', '0901234566', 'phuong.vu@gmail.com', N'34 Nguyễn Trãi, Thanh Xuân, Hà Nội', 0, 'hashed_password', NULL, 0, NULL),
('dinhvan_g', N'Đinh Văn Giang', '0901234567', 'giang.dinh@gmail.com', N'56 Bùi Thị Xuân, Đà Lạt, Lâm Đồng', 0, 'hashed_password', NULL, 0, NULL),
('buihoa_h', N'Bùi Hoa Hậu', '0901234568', 'hau.bui@gmail.com', N'78 Hùng Vương, Ninh Kiều, Cần Thơ', 0, 'hashed_password', NULL, 0, NULL),
('doanminh_i', N'Đoàn Minh Ý', '0901234569', 'y.doan@gmail.com', N'90 Lý Thường Kiệt, TP. Vũng Tàu', 0, 'hashed_password', NULL, 0, NULL),
('ngothuy_k', N'Ngô Thùy Kiều', '0901234570', 'kieu.ngo@gmail.com', N'11 Tôn Đức Thắng, Ba Đình, Hà Nội', 0, 'hashed_password', NULL, 0, NULL),
-- Nhân viên (UserType = 1)
('staff_nguyen', N'Nguyễn Văn Hùng', '0912345601', 'hung.nguyen@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password1', 15000000, 0, 1), -- Quản lý
('staff_tran', N'Trần Thị Lan', '0912345602', 'lan.tran@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password2', 8000000, 0, 2), -- Nhân viên phục vụ
('staff_pham', N'Phạm Văn Nam', '0912345603', 'nam.pham@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password3', 9000000, 0, 3), -- Đầu bếp
('staff_le', N'Lê Thị Mai', '0912345604', 'mai.le@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password4', 8500000, 0, 2), -- Nhân viên phục vụ
('staff_hoang', N'Hoàng Văn Khánh', '0912345605', 'khanh.hoang@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password5', 8700000, 0, 2), -- Nhân viên phục vụ
('staff_vu', N'Vũ Minh Tâm', '0912345606', 'tam.vu@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password6', 9200000, 0, 3), -- Đầu bếp
('staff_dinh', N'Đinh Thị Hồng', '0912345607', 'hong.dinh@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password7', 8300000, 0, 2), -- Nhân viên phục vụ
('staff_bui', N'Bùi Văn Long', '0912345608', 'long.bui@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password8', 8800000, 0, 3), -- Đầu bếp
('staff_doan', N'Đoàn Thị Ngọc', '0912345609', 'ngoc.doan@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password9', 8600000, 0, 2), -- Nhân viên phục vụ
('staff_ngo', N'Ngô Văn Quang', '0912345610', 'quang.ngo@xai.com', N'Nhà hàng, Hà Nội', 1, 'hashed_password10', 8900000, 0, 3); -- Đầu bếp

GO
-- Thêm dữ liệu vào bảng Areas
INSERT INTO Areas (AreaName, Description) VALUES
(N'Tầng 1', N'Khu vực tầng trệt gần cửa chính, sôi động'),
(N'Tầng 2', N'Khu vực lầu 1 yên tĩnh, phù hợp gia đình'),
(N'Sân vườn', N'Khu vực ngoài trời thoáng mát, gần gũi thiên nhiên');

GO
-- Thêm dữ liệu vào bảng Tables
INSERT INTO Tables (TableName, AreaID, Capacity, Status) VALUES
(N'Bàn 01', 1, 4, 0), -- Trống
(N'Bàn 02', 1, 6, 0),
(N'Bàn 03', 1, 8, 1), -- Đã đặt trước
(N'Bàn 04', 2, 4, 0),
(N'Bàn 05', 2, 6, 2), -- Đang sử dụng
(N'Bàn 06', 2, 10, 0),
(N'Bàn 07', 3, 4, 0),
(N'Bàn 08', 3, 6, 1);

GO
-- Thêm dữ liệu vào bảng Reservations
INSERT INTO Reservations (CustomerID, GuestName, PhoneNumber, TableID, CreatedTime, ReservationTime, Status, Notes) VALUES
(1, N'Nguyễn Văn An', '0901234561', 3, '2025-04-26 10:00:00', '2025-04-26 12:30:00', 2, N'Đặt cho bữa trưa gia đình'),
(2, N'Trần Thị Bình', '0901234562', 8, '2025-04-26 11:00:00', '2025-04-26 13:00:00', 1, N'Đặt cho nhóm bạn'),
(3, N'Phạm Văn Cường', '0901234563', 5, '2025-04-26 12:00:00', '2025-04-26 14:00:00', 2, N'Đặt cho buổi họp mặt'),
(NULL, N'Khách vãng lai', '0901234571', 1, '2025-04-26 13:00:00', '2025-04-26 15:00:00', 0, N'Khách hủy vì thay đổi kế hoạch');

GO
-- Thêm dữ liệu vào bảng Categories
INSERT INTO Categories (CategoryName) VALUES
(N'Món khai vị'),
(N'Món chính'),
(N'Tráng miệng'),
(N'Đồ uống');

GO
-- Thêm dữ liệu vào bảng MenuItems
INSERT INTO MenuItems (Name, MenuItemImage, CategoryID, Price, Description, Status) VALUES
(N'Gỏi cuốn tôm thịt', 'goi_cuon.jpg', 1, 50000, N'Gỏi cuốn tươi với tôm, thịt heo, rau sống và bún', 1),
(N'Súp cua', 'sup_cua.jpg', 1, 60000, N'Súp cua thơm ngon với trứng, thịt cua và nấm', 1),
(N'Phở bò tái', 'pho_bo.jpg', 2, 85000, N'Phở bò truyền thống với thịt tái, nước dùng đậm đà', 1),
(N'Cơm chiên dương châu', 'com_chien.jpg', 2, 70000, N'Cơm chiên với tôm, trứng, xúc xích và rau củ', 1),
(N'Bún bò Huế', 'bun_bo.jpg', 2, 90000, N'Bún bò Huế cay nồng với thịt bò, chả và rau thơm', 1),
(N'Mì xào hải sản', 'mi_xao.jpg', 2, 95000, N'Mì xào giòn với tôm, mực, cá viên và rau củ', 1),
(N'Lẩu thái hải sản', 'lau_thai.jpg', 2, 250000, N'Lẩu thái chua cay với tôm, mực, cá và nấm', 1),
(N'Chè ba màu', 'che_ba_mau.jpg', 3, 35000, N'Chè ba màu ngọt mát với đậu đỏ, lá dứa và nước cốt dừa', 1),
(N'Bánh flan', 'banh_flan.jpg', 3, 30000, N'Bánh flan caramel mềm mịn, thơm vị trứng', 1),
(N'Nước ép cam', 'nuoc_ep_cam.jpg', 4, 45000, N'Nước ép cam tươi nguyên chất, giàu vitamin C', 1),
(N'Trà đào cam sả', 'tra_dao.jpg', 4, 50000, N'Trà đào thơm mát với cam, sả và đào ngâm', 1),
(N'Cà phê sữa đá', 'ca_phe_sua.jpg', 4, 40000, N'Cà phê sữa đá truyền thống, đậm vị Việt', 1),
(N'Sinh tố bơ', 'sinh_to_bo.jpg', 4, 55000, N'Sinh tố bơ béo ngậy, thơm ngon', 1),
(N'Salad trộn', 'salad.jpg', 1, 65000, N'Salad rau củ tươi với sốt dầu giấm và mè rang', 1),
(N'Gà nướng muối ớt', 'ga_nuong.jpg', 2, 120000, N'Gà nướng thơm lừng với muối ớt, da giòn thịt mềm', 1),
(N'Cá kho tộ', 'ca_kho_to.jpg', 2, 110000, N'Cá kho tộ đậm đà với nước mắm, tiêu và hành', 1),
(N'Tôm rang muối hồng kông', 'tom_rang.jpg', 2, 150000, N'Tôm rang muối hồng kông giòn rụm, đậm vị', 1),
(N'Kem dâu', 'kem_dau.jpg', 3, 40000, N'Kem dâu tây mát lạnh, vị ngọt tự nhiên', 1),
(N'Nước dừa tươi', 'nuoc_dua.jpg', 4, 35000, N'Nước dừa tươi ngọt thanh, giải nhiệt', 1),
(N'Trà sữa trân châu', 'tra_sua.jpg', 4, 50000, N'Trà sữa trân châu thơm béo, trân châu dai giòn', 1);
GO

-- Thêm dữ liệu vào bảng Labels
INSERT INTO Labels (Name, Description) VALUES
('Bán chạy', 'Best Seller'),
('Mới', 'New'),
('Đặc biệt', 'Special');
GO

-- Thêm dữ liệu vào bảng MenuItemLabels
INSERT INTO MenuItemLabels (MenuItemID, LabelID) VALUES
(1, 1), -- Gỏi cuốn: Bán chạy
(3, 1), -- Phở bò: Bán chạy
(5, 2), -- Bún bò: Mới
(7, 3), -- Lẩu thái: Đặc biệt
(11, 2), -- Trà đào: Mới
(15, 1), -- Gà nướng: Bán chạy
(20, 2); -- Trà sữa: Mới

GO
-- Thêm dữ liệu vào bảng MenuItemAttributes
INSERT INTO MenuItemAttributes (MenuItemID, AttributeName, AttributeValue) VALUES
(1, N'Calories', N'150 kcal'),
(3, N'Spiciness', N'Không cay'),
(5, N'Spiciness', N'Cay vừa'),
(7, N'Phù hợp', N'Nhóm 4-6 người'),
(11, N'Caffeine', N'Có'),
(15, N'Thời gian chế biến', N'20 phút'),
(20, N'Caffeine', N'Có');

GO
-- Thêm dữ liệu vào bảng MenuItemRatings
INSERT INTO MenuItemRatings (MenuItemID, UserID, Rating, Comment, CreatedAt) VALUES
(1, 1, 5, N'Gỏi cuốn tươi ngon, nước chấm đậm đà!', '2025-04-26 12:30:00'),
(3, 2, 4, N'Phở thơm, nhưng nước dùng hơi nhạt.', '2025-04-26 13:00:00'),
(5, 3, 5, N'Bún bò Huế cay đúng vị, rất đáng thử!', '2025-04-26 14:00:00'),
(11, 4, 3, N'Trà đào ngon nhưng hơi ngọt.', '2025-04-26 15:00:00');

-- Thêm dữ liệu vào bảng Orders (20 đơn hàng)
INSERT INTO Orders (CustomerID, EmployeeID, TableID, OrderTime, Status, PaymentMethod) VALUES
(1, 11, 1, '2025-04-26 12:00:00', 3, N'Tiền mặt'), -- Hoàn thành
(2, 12, 2, '2025-04-26 12:15:00', 3, N'Thẻ tín dụng'),
(3, 13, 3, '2025-04-26 12:30:00', 2, N'Momo'), -- Đang chuẩn bị
(4, 14, 4, '2025-04-26 12:45:00', 2, N'Tiền mặt'),
(5, 15, 5, '2025-04-26 13:00:00', 1, N'Thẻ tín dụng'), -- Chờ xác nhận
(6, 16, 6, '2025-04-26 13:15:00', 1, N'Momo'),
(7, 17, 7, '2025-04-26 13:30:00', 0, N'Tiền mặt'), -- Đã hủy
(8, 18, 8, '2025-04-26 13:45:00', 3, N'Thẻ tín dụng'),
(9, 19, 1, '2025-04-26 14:00:00', 2, N'Momo'),
(10, 20, 2, '2025-04-26 14:15:00', 1, N'Tiền mặt'),
(1, 11, 3, '2025-04-26 14:30:00', 3, N'Thẻ tín dụng'),
(2, 12, 4, '2025-04-26 14:45:00', 2, N'Momo'),
(3, 13, 5, '2025-04-26 15:00:00', 1, N'Tiền mặt'),
(4, 14, 6, '2025-04-26 15:15:00', 0, N'Thẻ tín dụng'), -- Đã hủy
(5, 15, 7, '2025-04-26 15:30:00', 3, N'Momo'),
(6, 16, 8, '2025-04-26 15:45:00', 2, N'Tiền mặt'),
(7, 17, 1, '2025-04-26 16:00:00', 1, N'Thẻ tín dụng'),
(8, 18, 2, '2025-04-26 16:15:00', 3, N'Momo'),
(9, 19, 3, '2025-04-26 16:30:00', 2, N'Tiền mặt'),
(10, 20, 4, '2025-04-26 16:45:00', 1, N'Thẻ tín dụng');

GO
-- Thêm dữ liệu vào bảng OrderDetails (mỗi đơn hàng có 2-4 món)
INSERT INTO OrderDetails (OrderID, MenuItemID, Quantity) VALUES
(1, 1, 2), (1, 3, 1), (1, 10, 2), -- Gỏi cuốn, Phở bò, Nước ép cam
(2, 2, 1), (2, 4, 2), (2, 11, 1), (2, 15, 1), -- Súp cua, Cơm chiên, Trà đào, Gà nướng
(3, 5, 1), (3, 7, 1), (3, 12, 2), -- Bún bò, Lẩu thái, Cà phê sữa
(4, 6, 2), (4, 8, 1), (4, 13, 1), -- Mì xào, Chè ba màu, Sinh tố bơ
(5, 9, 2), (5, 14, 1), (5, 16, 1), (5, 20, 1), -- Bánh flan, Salad, Cá kho, Trà sữa
(6, 1, 1), (6, 17, 1), (6, 19, 2), -- Gỏi cuốn, Tôm rang, Nước dừa
(7, 3, 1), (7, 5, 1), (7, 11, 1), -- Phở bò, Bún bò, Trà đào
(8, 4, 2), (8, 6, 1), (8, 12, 1), (8, 18, 1), -- Cơm chiên, Mì xào, Cà phê, Kem dâu
(9, 7, 1), (9, 9, 1), (9, 15, 1), -- Lẩu thái, Bánh flan, Gà nướng
(10, 2, 1), (10, 8, 1), (10, 10, 2), (10, 20, 1), -- Súp cua, Chè ba màu, Nước ép cam, Trà sữa
(11, 1, 2), (11, 3, 1), (11, 12, 1), -- Gỏi cuốn, Phở bò, Cà phê
(12, 5, 1), (12, 7, 1), (12, 13, 2), (12, 19, 1), -- Bún bò, Lẩu thái, Sinh tố bơ, Nước dừa
(13, 4, 1), (13, 6, 1), (13, 11, 1), -- Cơm chiên, Mì xào, Trà đào
(14, 2, 1), (14, 8, 1), (14, 14, 1), (14, 20, 1), -- Súp cua, Chè ba màu, Salad, Trà sữa
(15, 9, 1), (15, 15, 1), (15, 17, 1), -- Bánh flan, Gà nướng, Tôm rang
(16, 1, 1), (16, 3, 1), (16, 10, 2), (16, 18, 1), -- Gỏi cuốn, Phở bò, Nước ép cam, Kem dâu
(17, 5, 1), (17, 7, 1), (17, 12, 1), -- Bún bò, Lẩu thái, Cà phê
(18, 4, 1), (18, 6, 1), (18, 11, 1), (18, 19, 1), -- Cơm chiên, Mì xào, Trà đào, Nước dừa
(19, 2, 1), (19, 8, 1), (19, 13, 1), -- Súp cua, Chè ba màu, Sinh tố bơ
(20, 1, 1), (20, 9, 1), (20, 15, 1), (20, 20, 1); -- Gỏi cuốn, Bánh flan, Gà nướng, Trà sữa

GO
-- Thêm dữ liệu vào bảng TrustedDevices (cho nhân viên)
INSERT INTO TrustedDevices (UserId, DeviceToken, CreatedAt) VALUES
(11, 'device_token_1', '2025-04-26 09:00:00'), -- Quản lý
(12, 'device_token_2', '2025-04-26 09:15:00'), -- Nhân viên phục vụ
(13, 'device_token_3', '2025-04-26 09:30:00'), -- Đầu bếp
(14, 'device_token_4', '2025-04-26 09:45:00'), -- Nhân viên phục vụ
(15, 'device_token_5', '2025-04-26 10:00:00'), -- Nhân viên phục vụ
(16, 'device_token_6', '2025-04-26 10:15:00'), -- Đầu bếp
(17, 'device_token_7', '2025-04-26 10:30:00'), -- Nhân viên phục vụ
(18, 'device_token_8', '2025-04-26 10:45:00'), -- Đầu bếp
(19, 'device_token_9', '2025-04-26 11:00:00'), -- Nhân viên phục vụ
(20, 'device_token_10', '2025-04-26 11:15:00'); -- Đầu bếp

GO
-- DROP TABLE IF EXISTS MenuItemAttributes;
-- DROP TABLE IF EXISTS MenuItemRatings;
-- DROP TABLE IF EXISTS MenuItemLabels;
-- DROP TABLE IF EXISTS OrderDetails;
-- DROP TABLE IF EXISTS Orders;
-- DROP TABLE IF EXISTS Reservations;
-- DROP TABLE IF EXISTS Tables;
-- DROP TABLE IF EXISTS Areas;
-- DROP TABLE IF EXISTS TrustedDevices;
-- DROP TABLE IF EXISTS Users;
-- DROP TABLE IF EXISTS RolePermissions;
-- DROP TABLE IF EXISTS Permissions;
-- DROP TABLE IF EXISTS Roles;
-- DROP TABLE IF EXISTS MenuItems;
-- DROP TABLE IF EXISTS Categories;
-- DROP TABLE IF EXISTS Labels;
