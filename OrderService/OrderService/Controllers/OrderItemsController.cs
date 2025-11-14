using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("order_items")]
public class OrderItemsController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _http;

    public OrderItemsController(OrderDbContext db, IHttpClientFactory http)
    {
        _db = db;
        _http = http;
    }

    // ---------------- DTOs ----------------
    public record OrderItemDto(
        int Id,
        int OrderId,
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    public class OrderItemCreateDto
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class OrderItemUpdateDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    private record ProductDto(int id, string name, string? description, decimal price, int quantity);

    private static OrderItemDto Map(OrderItem i) => new(
        i.Id, i.OrderId, i.ProductId, i.ProductName,
        i.Quantity, i.UnitPrice, i.TotalPrice,
        i.CreatedAt, i.UpdatedAt
    );

    // ---------------- GET ----------------
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.OrderItems.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => Map(x))
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.OrderItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : Ok(Map(item));
    }

    // ---------------- CREATE ----------------
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] OrderItemCreateDto dto)
    {
        if (dto.OrderId <= 0) return BadRequest(new { message = "OrderId is required" });
        if (dto.ProductId <= 0) return BadRequest(new { message = "ProductId is required" });
        if (dto.Quantity <= 0) return BadRequest(new { message = "Quantity must be > 0" });

        // Kiểm tra đơn hàng tồn tại
        var orderExists = await _db.Orders.AsNoTracking().AnyAsync(o => o.Id == dto.OrderId);
        if (!orderExists) return BadRequest(new { message = $"Order {dto.OrderId} not found" });

        // Gọi ProductService để kiểm tra tồn kho
        var client = _http.CreateClient("products");
        var p = await client.GetFromJsonAsync<ProductDto>($"/products/{dto.ProductId}");
        if (p is null)
            return BadRequest(new { message = $"Product {dto.ProductId} not found" });

        if (p.quantity < dto.Quantity)
            return BadRequest(new { message = $"Insufficient stock for product {p.name} (only {p.quantity} left)" });

        // Điền lại tên và giá nếu FE không truyền
        var now = DateTime.UtcNow;
        var item = new OrderItem
        {
            OrderId = dto.OrderId,
            ProductId = dto.ProductId,
            ProductName = string.IsNullOrWhiteSpace(dto.ProductName) ? p.name : dto.ProductName!,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice is > 0 ? dto.UnitPrice.Value : p.price,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.OrderItems.Add(item);
        await _db.SaveChangesAsync();

        // Lấy lại bản ghi vừa tạo (vì TotalPrice có thể được DB tính)
        var created = await _db.OrderItems.AsNoTracking().FirstAsync(x => x.Id == item.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    // ---------------- UPDATE ----------------
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] OrderItemUpdateDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "Id mismatch" });
        if (dto.Quantity <= 0) return BadRequest(new { message = "Quantity must be > 0" });

        var item = await _db.OrderItems.FindAsync(id);
        if (item is null) return NotFound();

        // Nếu product/quantity/price thay đổi → gọi ProductService kiểm tra tồn kho
        if (dto.ProductId != item.ProductId || dto.Quantity != item.Quantity)
        {
            var client = _http.CreateClient("products");
            var p = await client.GetFromJsonAsync<ProductDto>($"/products/{dto.ProductId}");
            if (p is null)
                return BadRequest(new { message = $"Product {dto.ProductId} not found" });
            if (p.quantity < dto.Quantity)
                return BadRequest(new { message = $"Insufficient stock for product {p.name}" });

            item.ProductName = string.IsNullOrWhiteSpace(dto.ProductName) ? p.name : dto.ProductName!;
            item.UnitPrice = dto.UnitPrice is > 0 ? dto.UnitPrice.Value : p.price;
        }
        else
        {
            // Không đổi Product → chỉ cập nhật giá trị mới từ FE nếu có
            item.ProductName = string.IsNullOrWhiteSpace(dto.ProductName) ? item.ProductName : dto.ProductName!;
            if (dto.UnitPrice is > 0) item.UnitPrice = dto.UnitPrice.Value;
        }

        item.ProductId = dto.ProductId;
        item.Quantity = dto.Quantity;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- DELETE ----------------
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var i = await _db.OrderItems.FindAsync(id);
        if (i is null) return NotFound();
        _db.OrderItems.Remove(i);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
