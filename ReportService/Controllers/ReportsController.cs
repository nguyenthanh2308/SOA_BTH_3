// Controllers/ReportsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportService.Data;
using ReportService.DTOs;
using ReportService.Models;
using ReportService.Services;

namespace ReportService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ReportDbContext _context;
        private readonly IExternalService _externalService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ReportDbContext context, 
            IExternalService externalService,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _externalService = externalService;
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                var report = new Report
                {
                    ReportType = request.ReportType,
                    Period = request.StartDate,
                    GeneratedAt = DateTime.UtcNow
                };

                if (request.ReportType == "Product")
                {
                    await GenerateProductReport(report, request);
                }
                else if (request.ReportType == "Order")
                {
                    await GenerateOrderReport(report, request);
                }
                else
                {
                    return BadRequest("Invalid report type. Must be 'Product' or 'Order'.");
                }

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Report generated successfully", ReportId = report.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, "An error occurred while generating the report");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReportDto>> GetReport(int id)
        {
            var report = await _context.Reports
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            return new ReportDto
            {
                Id = report.Id,
                ReportType = report.ReportType,
                Period = report.Period,
                GeneratedAt = report.GeneratedAt,
                Details = report.Details.Select(d => new ReportDetailDto
                {
                    Key = d.Key,
                    Name = d.Name,
                    Quantity = d.Quantity,
                    Value = d.Value
                }).ToList()
            };
        }

        [HttpGet("products/stats")]
        public async Task<IActionResult> GetProductStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var products = await _externalService.GetProductsAsync();
                var orderItems = await _externalService.GetOrderItemsAsync(startDate.Value, endDate.Value);

                var productStats = products.Select(p => new
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CurrentStock = p.Quantity,
                    TotalSold = orderItems
                        .Where(oi => oi.ProductId == p.Id)
                        .Sum(oi => oi.Quantity),
                    TotalRevenue = orderItems
                        .Where(oi => oi.ProductId == p.Id)
                        .Sum(oi => oi.Quantity * oi.UnitPrice),
                    TotalCost = orderItems
                        .Where(oi => oi.ProductId == p.Id)
                        .Sum(oi => oi.Quantity * oi.UnitPrice * 0.8m), // Giả sử cost = 80% giá bán
                    TotalProfit = orderItems
                        .Where(oi => oi.ProductId == p.Id)
                        .Sum(oi => oi.Quantity * oi.UnitPrice * 0.2m) // Profit = 20% giá bán
                }).ToList();

                return Ok(productStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product stats");
                return StatusCode(500, "An error occurred while retrieving product statistics");
            }
        }

        [HttpGet("orders/summary")]
        public async Task<IActionResult> GetOrderSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var orders = await _externalService.GetOrdersAsync(startDate.Value, endDate.Value);
                var orderItems = await _externalService.GetOrderItemsAsync(startDate.Value, endDate.Value);

                var totalCost = orderItems.Sum(oi => oi.Quantity * oi.UnitPrice * 0.8m); // Giả sử cost = 80% giá bán
                var totalRevenue = orders.Sum(o => o.TotalAmount);
                var orderStats = new
                {
                    TotalOrders = orders.Count,
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    TotalProfit = totalRevenue - totalCost,
                    AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                    OrdersByDate = orders
                        .GroupBy(o => o.CreatedAt.Date)
                        .Select(g => new
                        {
                            Date = g.Key,
                            Count = g.Count(),
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Date)
                        .ToList()
                };

                return Ok(orderStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order summary");
                return StatusCode(500, "An error occurred while retrieving order summary");
            }
        }

        private async Task GenerateProductReport(Report report, GenerateReportRequest request)
        {
            var products = await _externalService.GetProductsAsync();
            var orderItems = await _externalService.GetOrderItemsAsync(request.StartDate, request.EndDate);

            foreach (var product in products)
            {
                var items = orderItems.Where(oi => oi.ProductId == product.Id).ToList();
                var totalSold = items.Sum(oi => oi.Quantity);
                var totalRevenue = items.Sum(oi => oi.Quantity * oi.UnitPrice);
                var totalCost = items.Sum(oi => oi.Quantity * oi.UnitPrice * 0.8m); // Giả sử cost = 80% giá bán
                var totalProfit = totalRevenue - totalCost;

                report.Details.Add(new ReportDetail
                {
                    Key = product.Id.ToString(),
                    Name = product.Name,
                    Quantity = totalSold,
                    Value = totalProfit // Lợi nhuận
                });
            }
        }

        private async Task GenerateOrderReport(Report report, GenerateReportRequest request)
        {
            var orders = await _externalService.GetOrdersAsync(request.StartDate, request.EndDate);
            var orderItems = await _externalService.GetOrderItemsAsync(request.StartDate, request.EndDate);

            foreach (var order in orders)
            {
                var items = orderItems.Where(oi => oi.OrderId == order.Id).ToList();
                var totalRevenue = items.Sum(oi => oi.Quantity * oi.UnitPrice);
                var totalCost = items.Sum(oi => oi.Quantity * oi.UnitPrice * 0.8m); // Giả sử cost = 80% giá bán
                var totalProfit = totalRevenue - totalCost;

                report.Details.Add(new ReportDetail
                {
                    Key = order.Id.ToString(),
                    Name = $"Order #{order.Id}",
                    Quantity = items.Sum(oi => oi.Quantity),
                    Value = totalProfit // Lợi nhuận
                });
            }
        }
    }
}