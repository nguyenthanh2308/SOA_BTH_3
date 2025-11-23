// Services/ExternalService.cs
using ReportService.DTOs;
using ReportService.Services;

namespace ReportService.Services
{
    public class ExternalService : IExternalService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ExternalService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            var client = _httpClientFactory.CreateClient("external");
            var response = await client.GetAsync($"{_configuration["Services:Product"]}/products");
            response.EnsureSuccessStatusCode();
            return await response.ReadAsAsync<List<ProductDto>>();
        }

        public async Task<List<OrderDto>> GetOrdersAsync(DateTime startDate, DateTime endDate)
        {
            var client = _httpClientFactory.CreateClient("external");
            var response = await client.GetAsync(
                $"{_configuration["Services:Order"]}/orders/by-date?" +
                $"startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            response.EnsureSuccessStatusCode();
            return await response.ReadAsAsync<List<OrderDto>>();
        }

        public async Task<List<OrderItemDto>> GetOrderItemsAsync(DateTime startDate, DateTime endDate)
        {
            var client = _httpClientFactory.CreateClient("external");
            var response = await client.GetAsync(
                $"{_configuration["Services:Order"]}/order_items/by-date?" +
                $"startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            response.EnsureSuccessStatusCode();
            return await response.ReadAsAsync<List<OrderItemDto>>();
        }
    }
}