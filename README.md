# E-Commerce Microservices Application

Ứng dụng E-Commerce được xây dựng theo kiến trúc Microservices với ASP.NET Core và Client UI.

## 📋 Tổng quan

Project bao gồm 4 microservices độc lập và 1 Client UI:

### 1. **AuthService** (Dịch vụ Xác thực)
- **Chức năng**: Quản lý đăng nhập và xác thực người dùng
- **Port**: 5001
- **Tính năng**:
  - Đăng nhập với username/password
  - Xác thực MD5 hash password
  - Phân quyền User/Admin
  - Tạo và quản lý JWT token

### 2. **ProductService** (Dịch vụ Sản phẩm)
- **Chức năng**: Quản lý danh mục sản phẩm
- **Port**: 5002
- **Database**: `Product_service_db`
- **Tính năng**:
  - CRUD sản phẩm (Tạo, Đọc, Cập nhật, Xóa)
  - Quản lý tồn kho
  - Quản lý giá sản phẩm
  - Tìm kiếm và lọc sản phẩm
  - Tracking thời gian tạo/cập nhật

### 3. **OrderService** (Dịch vụ Đơn hàng)
- **Chức năng**: Quản lý đơn hàng và khách hàng
- **Port**: 5003
- **Database**: `Order_services_db`
- **Tính năng**:
  - Tạo đơn hàng mới
  - Quản lý chi tiết đơn hàng (OrderItems)
  - Quản lý khách hàng (Customer)
  - Cập nhật trạng thái đơn hàng (pending, processing, completed, cancelled)
  - Tính toán tổng giá trị đơn hàng
  - Quản lý thông tin khách hàng với email duy nhất

### 4. **ReportService** (Dịch vụ Báo cáo)
- **Chức năng**: Tạo báo cáo và thống kê
- **Port**: 5004
- **Database**: `Report_service_db` (SQLite)
- **Tính năng**:
  - Tạo báo cáo sản phẩm (Product Report)
  - Tạo báo cáo đơn hàng (Order Report)
  - Thống kê sản phẩm theo period
  - Thống kê đơn hàng theo period
  - Lưu trữ lịch sử báo cáo
  - Chi tiết báo cáo với key-value metrics

### 5. **EcommerceClientUI** (Giao diện Client)
- **Công nghệ**: ASP.NET Core MVC + Static HTML/CSS/JavaScript
- **Port**: 5000
- **Tính năng**:
  - **Trang đăng nhập** (`login.html`): Xác thực người dùng với JWT
  - **Trang chủ** (`index.html`): Hiển thị danh sách sản phẩm
  - **Trang shop** (`shop.html`): Mua sắm và giỏ hàng
  - **Trang quản trị** (`admin.html`): 
    - Quản lý sản phẩm (CRUD)
    - Quản lý đơn hàng
    - Tạo báo cáo và thống kê
    - Chỉ dành cho Admin

## 🗄️ Database Schema

### Database 1: `Product_service_db` (MySQL)

#### Bảng: `products`
| Cột | Kiểu | Mô tả |
|-----|------|-------|
| `id` | INT (PK, Auto) | ID sản phẩm |
| `name` | VARCHAR(255) | Tên sản phẩm (bắt buộc) |
| `description` | VARCHAR(1000) | Mô tả sản phẩm |
| `price` | DECIMAL(12,2) | Giá sản phẩm (bắt buộc) |
| `quantity` | INT | Số lượng tồn kho (bắt buộc) |
| `created_at` | DATETIME | Thời gian tạo |
| `updated_at` | DATETIME | Thời gian cập nhật cuối |

### Database 2: `Order_services_db` (MySQL)

#### Bảng: `customers`
| Cột | Kiểu | Mô tả |
|-----|------|-------|
| `id` | INT (PK, Auto) | ID khách hàng |
| `full_name` | VARCHAR(255) | Tên đầy đủ (bắt buộc) |
| `email` | VARCHAR(255) | Email (bắt buộc, unique) |
| `password_md5` | VARCHAR(32) | Hash MD5 của mật khẩu |
| `created_at` | DATETIME | Thời gian tạo tài khoản |

**Relationships**: 1 Customer → N Orders

#### Bảng: `orders`
| Cột | Kiểu | Mô tả |
|-----|------|-------|
| `Id` | INT (PK, Auto) | ID đơn hàng |
| `CustomerName` | NVARCHAR | Tên khách hàng |
| `CustomerEmail` | NVARCHAR | Email khách hàng |
| `Status` | NVARCHAR | Trạng thái (pending/processing/completed/cancelled) |
| `TotalAmount` | DECIMAL | Tổng giá trị đơn hàng |
| `CreatedAt` | DATETIME | Thời gian tạo |
| `UpdatedAt` | DATETIME | Thời gian cập nhật |
| `customer_id` | INT (FK) | ID khách hàng (nullable) |

**Relationships**: 1 Order → N OrderItems, N Orders → 1 Customer

#### Bảng: `order_items`
| Cột | Kiểu | Mô tả |
|-----|------|-------|
| `Id` | INT (PK, Auto) | ID item |
| `OrderId` | INT (FK) | ID đơn hàng |
| `ProductId` | INT | ID sản phẩm (reference đến ProductService) |
| `ProductName` | NVARCHAR | Tên sản phẩm |
| `Quantity` | INT | Số lượng |
| `UnitPrice` | DECIMAL | Giá đơn vị |
| `TotalPrice` | DECIMAL (Computed) | Tổng giá (Quantity × UnitPrice) |
| `CreatedAt` | DATETIME | Thời gian tạo |
| `UpdatedAt` | DATETIME | Thời gian cập nhật |

**Relationships**: N OrderItems → 1 Order

### Database 3: `Report_service_db` (SQLite)

#### Bảng: `Reports`
| Cột | Kiểu | Mô tả |
|-----|------|-------|
| `Id` | INTEGER (PK, Auto) | ID báo cáo |
| `ReportType` | TEXT(50) | Loại báo cáo ("Product" hoặc "Order") |
| `Period` | DATETIME | Kỳ báo cáo (ngày/tháng/năm) |
| `GeneratedAt` | DATETIME | Thời gian tạo báo cáo |

**Relationships**: 1 Report → N ReportDetails

#### Bảng: `ReportDetails`
| Cột | Kiểu | Mô tả |
|-----|------|-------|
| `Id` | INTEGER (PK, Auto) | ID chi tiết |
| `ReportId` | INTEGER (FK) | ID báo cáo |
| `Key` | TEXT | ProductId hoặc OrderId |
| `Name` | TEXT | Tên sản phẩm hoặc mã đơn hàng |
| `Quantity` | DECIMAL | Số lượng |
| `Value` | DECIMAL | Giá trị (doanh thu/chi phí/lợi nhuận) |

**Relationships**: N ReportDetails → 1 Report

## 🚀 Cách chạy Project

### Sử dụng Script tự động (Khuyến nghị)

**Windows (PowerShell)**:
```powershell
.\start-all-services.ps1
```

**Windows (Command Prompt/Batch)**:
```batch
start-all-services.bat
```

### Chạy thủ công từng service

```bash
# Terminal 1: Auth Service
cd AuthService/AuthService
dotnet run

# Terminal 2: Product Service  
cd ProductService/ProductService
dotnet run

# Terminal 3: Order Service
cd OrderService/OrderService
dotnet run

# Terminal 4: Report Service
cd ReportService
dotnet run

# Terminal 5: Client UI
cd EcommerceClientUI/EcommerceClientUI
dotnet run
```

## 🌐 Service Endpoints

| Service | URL | Database |
|---------|-----|----------|
| Auth Service | http://localhost:5001 | In-memory |
| Product Service | http://localhost:5002 | MySQL (Product_service_db) |
| Order Service | http://localhost:5003 | MySQL (Order_services_db) |
| Report Service | http://localhost:5004 | SQLite (Report_service_db) |
| Client UI | http://localhost:5000 | N/A |

## 🔐 Thông tin đăng nhập mặc định

- **Admin**: 
  - Username: `admin`
  - Password: `admin123`
  
- **User**:
  - Username: `user`
  - Password: `user123`

## 🛠️ Công nghệ sử dụng

- **Backend Framework**: ASP.NET Core 8.0
- **Database**: 
  - MySQL (ProductService & OrderService)
  - SQLite (ReportService)
- **ORM**: Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Password Hashing**: MD5
- **Frontend**: HTML, CSS, JavaScript (Vanilla)
- **Architecture**: Microservices

## 📝 Lưu ý

- Đảm bảo MySQL Server đã được cài đặt và chạy
- Đảm bảo các port 5000-5004 không bị chiếm dụng
- Database sẽ tự động được tạo khi chạy migration lần đầu
- JWT token có thời hạn, cần đăng nhập lại khi hết hạn
