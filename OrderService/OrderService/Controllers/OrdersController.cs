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
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
    {
        if (dto?.Items is null || dto.Items.Count == 0)
            return BadRequest(new { message = "Order must have at least 1 item" });

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
            CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail,
            Status = "pending",
            CreatedAt = now,
            UpdatedAt = now, // 👈 tránh NULL cho cột NOT NULL
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
                UpdatedAt = now, // 👈 tránh NULL cho cột NOT NULL
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

    // ---------- UPDATE STATUS ----------
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderUpdateStatusDto dto)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o is null) return NotFound();

        var status = dto.Status?.Trim().ToLowerInvariant();
        if (status is not ("pending" or "completed" or "cancelled"))
            return BadRequest(new { message = "Invalid status" });

        o.Status = status!;
        o.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
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
}
