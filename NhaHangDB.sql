CREATE DATABASE RestaurantDB;
GO
USE RestaurantDB;
GO
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE NULL,
    Address NVARCHAR(255) NULL,
    
    UserType TINYINT NOT NULL CHECK (UserType IN (0,1)), -- 0 = Khách hàng, 1 = Nhân viên
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
    CustomerID INT NOT NULL, 
    TableID INT NOT NULL,
    CreatedTime DATETIME DEFAULT GETDATE() NOT NULL, -- Thời gian tạo đặt chỗ
    ReservationTime DATETIME NOT NULL, -- Thời gian khách đặt chỗ thực tế
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

CREATE TABLE Reviews (
    ReviewID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    MenuItemID INT NOT NULL,
    StarRating TINYINT NOT NULL CHECK (StarRating BETWEEN 1 AND 5), -- 1 đến 5 sao
    Comment NVARCHAR(500) NULL,
    ReviewTime DATETIME DEFAULT GETDATE() NOT NULL,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (MenuItemID) REFERENCES MenuItems(MenuItemID) ON DELETE CASCADE
);
