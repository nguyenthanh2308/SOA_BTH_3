using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("products/stock")]
    public class ProductStockController : ControllerBase
    {
        private readonly ProductDbContext _db;
        public ProductStockController(ProductDbContext db) => _db = db;

        // POST /products/stock/decrease
        [HttpPost("decrease")]
        // [Authorize]
        public async Task<IActionResult> Decrease([FromBody] StockAdjustRequest req)
        {
            if (req?.Items is null || req.Items.Count == 0)
                return BadRequest(new { message = "Request must have at least one item." });

            using var tx = await _db.Database.BeginTransactionAsync();

            var ids = req.Items.Select(x => x.ProductId).ToList();
            var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

            foreach (var it in req.Items)
            {
                if (it.Quantity <= 0)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { message = "Quantity must be > 0." });
                }

                var p = products.FirstOrDefault(x => x.Id == it.ProductId);
                if (p is null)
                {
                    await tx.RollbackAsync();
                    return NotFound(new { message = $"Product {it.ProductId} not found." });
                }

                if (p.Quantity < it.Quantity)
                {
                    await tx.RollbackAsync();
                    return StatusCode(StatusCodes.Status409Conflict,
                        new { message = $"Insufficient stock for product {p.Id} ({p.Name}). Left = {p.Quantity}, need = {it.Quantity}" });
                }
            }

            foreach (var it in req.Items)
            {
                var p = products.First(x => x.Id == it.ProductId);
                p.Quantity -= it.Quantity;
                p.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }

        // POST /products/stock/increase
        [HttpPost("increase")]
        // [Authorize]
        public async Task<IActionResult> Increase([FromBody] StockAdjustRequest req)
        {
            if (req?.Items is null || req.Items.Count == 0)
                return BadRequest(new { message = "Request must have at least one item." });

            using var tx = await _db.Database.BeginTransactionAsync();

            var ids = req.Items.Select(x => x.ProductId).ToList();
            var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

            foreach (var it in req.Items)
            {
                if (it.Quantity <= 0)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { message = "Quantity must be > 0." });
                }

                var p = products.FirstOrDefault(x => x.Id == it.ProductId);
                if (p is null)
                {
                    await tx.RollbackAsync();
                    return NotFound(new { message = $"Product {it.ProductId} not found." });
                }

                p.Quantity += it.Quantity;
                p.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }
    }
}
