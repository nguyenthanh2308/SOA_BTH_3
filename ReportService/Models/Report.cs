// Models/Report.cs
using System.ComponentModel.DataAnnotations;

namespace ReportService.Models
{
    public class Report
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } // "Product" hoặc "Order"
        
        [Required]
        public DateTime Period { get; set; } // Ngày/Tháng/Năm báo cáo
        
        [Required]
        public DateTime GeneratedAt { get; set; }
        
        public ICollection<ReportDetail> Details { get; set; } = new List<ReportDetail>();
    }

    public class ReportDetail
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public string? Key { get; set; } // ProductId hoặc OrderId
        public string? Name { get; set; } // Tên sản phẩm hoặc mã đơn hàng
        public decimal Quantity { get; set; } // Số lượng
        public decimal Value { get; set; } // Giá trị (doanh thu/chi phí/lợi nhuận)
        public Report Report { get; set; }
    }
}