using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("products")]
    public class ProductController : ControllerBase
    {
        private readonly ProductDbContext _db;
        public ProductController(ProductDbContext db) => _db = db;

        // GET /products
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _db.Products.AsNoTracking().ToListAsync());

        // GET /products/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return p is null ? NotFound(new { message = $"Product {id} not found" }) : Ok(p);
        }

        // POST /products
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] Product input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                return BadRequest(new { message = "Name is required" });

            _db.Products.Add(input);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        // PUT /products/{id}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] Product input)
        {
            if (id != input.Id) return BadRequest(new { message = "Id mismatch" });

            var p = await _db.Products.FindAsync(id);
            if (p is null) return NotFound();

            p.Name = input.Name;
            p.Description = input.Description;
            p.Price = input.Price;
            p.Quantity = input.Quantity;
            p.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /products/{id}
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p is null) return NotFound();

            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // LƯU Ý: không còn các endpoint /products/stock/decrease|increase ở đây.
    }
}
