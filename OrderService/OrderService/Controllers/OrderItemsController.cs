using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("order_items")]
public class OrderItemsController : ControllerBase
{
    private readonly OrderDbContext _db;
    public OrderItemsController(OrderDbContext db) => _db = db;

    // ===== DTOs =====
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
        public decimal UnitPrice { get; set; }
    }

    public class OrderItemUpdateDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // ===== Helpers =====
    private static OrderItemDto Map(OrderItem i) => new(
        i.Id, i.OrderId, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice,
        i.CreatedAt, i.UpdatedAt
    );

    // ===== GET ALL =====
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.OrderItems.AsNoTracking()
            .OrderByDescending(i => i.Id)
            .Select(i => new OrderItemDto(
                i.Id, i.OrderId, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice, i.CreatedAt, i.UpdatedAt
            ))
            .ToListAsync();

        return Ok(list);
    }

    // ===== GET BY ID =====
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var i = await _db.OrderItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return i is null ? NotFound() : Ok(Map(i));
    }

    // ===== CREATE =====
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] OrderItemCreateDto dto)
    {
        // Validate cơ bản
        if (dto.OrderId <= 0) return BadRequest(new { message = "OrderId is required" });
        if (dto.ProductId <= 0) return BadRequest(new { message = "ProductId is required" });
        if (dto.Quantity <= 0) return BadRequest(new { message = "Quantity must be > 0" });
        if (dto.UnitPrice < 0) return BadRequest(new { message = "UnitPrice must be >= 0" });

        // Order phải tồn tại
        var orderExists = await _db.Orders.AsNoTracking().AnyAsync(o => o.Id == dto.OrderId);
        if (!orderExists) return BadRequest(new { message = $"Order {dto.OrderId} not found" });

        var now = DateTime.UtcNow;

        var i = new OrderItem
        {
            OrderId = dto.OrderId,
            ProductId = dto.ProductId,
            ProductName = dto.ProductName ?? string.Empty,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            CreatedAt = now,
            UpdatedAt = now
            // ⚠️ KHÔNG gán TotalPrice — cột generated/trigger sẽ tự tính
        };

        _db.OrderItems.Add(i);
        await _db.SaveChangesAsync();

        // Reload không tracking để lấy TotalPrice computed từ DB
        var created = await _db.OrderItems.AsNoTracking().FirstAsync(x => x.Id == i.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    // ===== UPDATE =====
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] OrderItemUpdateDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "Id mismatch" });
        if (dto.Quantity <= 0) return BadRequest(new { message = "Quantity must be > 0" });
        if (dto.UnitPrice < 0) return BadRequest(new { message = "UnitPrice must be >= 0" });

        var i = await _db.OrderItems.FindAsync(id);
        if (i is null) return NotFound();

        i.ProductId = dto.ProductId;
        i.ProductName = dto.ProductName ?? string.Empty;
        i.Quantity = dto.Quantity;
        i.UnitPrice = dto.UnitPrice;
        i.UpdatedAt = DateTime.UtcNow; // tránh NULL & phản ánh cập nhật

        // ⚠️ KHÔNG set i.TotalPrice — DB sẽ tự tính (computed/trigger)
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== DELETE =====
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
