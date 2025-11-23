using OrderService.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _http;

    public OrdersController(OrderDbContext db, IHttpClientFactory http)
    {
        _db = db;
        _http = http;
    }

    // ---------- DTOs ----------
    public record OrderItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);

    public record OrderDto(
        int Id,
        string CustomerName,
        string CustomerEmail,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        List<OrderItemDto> Items);

    public class OrderCreateItemDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderCreateDto
    {
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public List<OrderCreateItemDto> Items { get; set; } = new();
    }

    public class OrderUpdateStatusDto
    {
        public string Status { get; set; } = "pending"; // pending | completed | cancelled
    }

    private record ProductDto(int id, string name, string? description, decimal price, int quantity);

    // ---------- GET ----------
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAll()
    {
        var list = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerName,
                o.CustomerEmail,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice
                )).ToList()
            ))
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var dto = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.Id == id)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerName,
                o.CustomerEmail,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice
                )).ToList()
            ))
            .FirstOrDefaultAsync();

        return dto is null ? NotFound() : Ok(dto);
    }

    // ---------- CREATE ----------
    // Allow anonymous customers to create orders (no admin JWT required)
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
    {
        if (dto?.Items is null || dto.Items.Count == 0)
            return BadRequest(new { message = "Order must have at least 1 item" });

        // Normalize customer email for lookup
        var customerEmailNorm = string.IsNullOrWhiteSpace(dto.CustomerEmail) ? null : dto.CustomerEmail.Trim().ToLowerInvariant();

        // Try to resolve CustomerId from provided dto.CustomerId or by matching email in customers table
        int? resolvedCustomerId = dto.CustomerId;
        if (resolvedCustomerId is null && customerEmailNorm is not null)
        {
            var existing = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Email == customerEmailNorm);
            if (existing is not null)
            {
                resolvedCustomerId = existing.Id;
                // fill name if missing
                if (string.IsNullOrWhiteSpace(dto.CustomerName)) dto.CustomerName = existing.FullName;
            }
        }

        // Kiểm tra tồn kho & bổ sung tên/giá từ ProductService (HttpClient đã cấu hình BaseAddress trong Program.cs)
        var client = _http.CreateClient("products");
        foreach (var it in dto.Items)
        {
            var p = await client.GetFromJsonAsync<ProductDto>($"/products/{it.ProductId}");
            if (p is null) return BadRequest(new { message = $"Product {it.ProductId} not found" });
            if (p.quantity < it.Quantity)
                return BadRequest(new { message = $"Insufficient stock for product {it.ProductId}" });

            if (string.IsNullOrWhiteSpace(it.ProductName)) it.ProductName = p.name;
            if (it.UnitPrice <= 0) it.UnitPrice = p.price;
        }

        var now = DateTime.UtcNow;

        var order = new Order
        {
            CustomerId = resolvedCustomerId,
            CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail,
            Status = "pending",
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var it in dto.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = it.ProductId,
                ProductName = it.ProductName ?? string.Empty,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Trả về DTO của bản ghi vừa tạo
        var created = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(x => x.Id == order.Id)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerName,
                o.CustomerEmail,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice
                )).ToList()
            ))
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ---------- UPDATE STATUS (đã thay mới để trừ/cộng kho) ----------
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderUpdateStatusDto dto)
    {
        // Load only needed columns (status + items) via projection to avoid selecting missing CustomerId column
        var o = await _db.Orders
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                Status = x.Status,
                Items = x.Items.Select(i => new { i.ProductId, i.Quantity }).ToList()
            })
            .FirstOrDefaultAsync();

        if (o is null) return NotFound();

        var from = o.Status?.Trim().ToLowerInvariant();
        var to = dto.Status?.Trim().ToLowerInvariant();

        if (to is not ("pending" or "completed" or "cancelled"))
            return BadRequest(new { message = "Invalid status" });

        // Không làm gì nếu không đổi trạng thái
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return NoContent();

        // Chuẩn bị payload gọi ProductService
        var stockReq = new
        {
            Items = o.Items.Select(i => new { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
        };

        var client = _http.CreateClient("products");

        // pending -> completed => TRỪ KHO
        if (from == "pending" && to == "completed")
        {
            var res = await client.PostAsJsonAsync("/products/stock/decrease", stockReq);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                // 409: không đủ hàng
                if ((int)res.StatusCode == 409)
                    return Conflict(new { message = "Not enough stock", detail = err });

                return BadRequest(new { message = "Decrease stock failed", detail = err });
            }
        }

        // completed -> cancelled => HOÀN KHO (compensation)
        if (from == "completed" && to == "cancelled")
        {
            var res = await client.PostAsJsonAsync("/products/stock/increase", stockReq);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return BadRequest(new { message = "Increase stock failed", detail = err });
            }
        }

        // Update status via raw SQL to avoid mapping columns that do not exist in DB
        var now = DateTime.UtcNow;
        var rows = await _db.Database.ExecuteSqlRawAsync(
            "UPDATE orders SET status = {0}, updated_at = {1} WHERE id = {2}",
            to, now, id);

        if (rows == 0) return NotFound();

        return NoContent();
    }

    // ---------- DELETE ----------
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o is null) return NotFound();
        _db.Orders.Remove(o);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /orders/by-customer/{customerId}?status=pending|completed
    [HttpGet("by-customer/{customerId:int}")]
    public async Task<IActionResult> GetByCustomer(int customerId, [FromQuery] string? status)
    {
        var q = _db.Orders.AsNoTracking()
                          .Include(o => o.Items)
                          .Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToLowerInvariant();
            if (st is "pending" or "completed" or "cancelled")
                q = q.Where(o => o.Status == st);
        }

        var list = await q
            .Select(o => new
            {
                o.Id,
                o.CustomerId,
                o.CustomerName,
                o.CustomerEmail,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                Items = o.Items.Select(i => new {
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    i.TotalPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET /orders/by-customer/{customerId}/history
    [HttpGet("by-customer/{customerId:int}/history")]
    public async Task<IActionResult> GetCustomerHistory(int customerId)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        var processing = orders.Where(o => o.Status == "pending" || o.Status == "cancelled");
        var completed = orders.Where(o => o.Status == "completed");

        return Ok(new
        {
            processing = processing.Select(o => new {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                Items = o.Items.Select(i => new { i.ProductName, i.Quantity, i.TotalPrice })
            }),
            completed = completed.Select(o => new {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                Items = o.Items.Select(i => new { i.ProductName, i.Quantity, i.TotalPrice })
            })
        });
    }

    // GET /orders/by-date?startDate=yyyy-MM-dd&endDate=yyyy-MM-dd
    [HttpGet("by-date")]
    public async Task<ActionResult<List<OrderDto>>> GetByDate([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var endDateTime = endDate.Value.Date.AddDays(1).AddSeconds(-1); // End of day
            query = query.Where(o => o.CreatedAt <= endDateTime);
        }

        var list = await query
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerName,
                o.CustomerEmail,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice
                )).ToList()
            ))
            .ToListAsync();

        return Ok(list);
    }

}
