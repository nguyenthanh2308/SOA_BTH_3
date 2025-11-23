// DTOs/ReportDtos.cs
using System.ComponentModel.DataAnnotations;

namespace ReportService.DTOs
{
    public class GenerateReportRequest
    {
        [Required]
        public string ReportType { get; set; } // "Product" hoặc "Order"
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
    }

    public class ReportDto
    {
        public int Id { get; set; }
        public string ReportType { get; set; }
        public DateTime Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<ReportDetailDto> Details { get; set; } = new();
    }

    public class ReportDetailDto
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal Value { get; set; }
    }

    // DTOs cho dữ liệu từ các service khác
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; } // Stock quantity
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}