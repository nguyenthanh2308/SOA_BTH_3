// Services/IExternalService.cs
using ReportService.DTOs;

namespace ReportService.Services
{
    public interface IExternalService
    {
        Task<List<ProductDto>> GetProductsAsync();
        Task<List<OrderDto>> GetOrdersAsync(DateTime startDate, DateTime endDate);
        Task<List<OrderItemDto>> GetOrderItemsAsync(DateTime startDate, DateTime endDate);
    }
}