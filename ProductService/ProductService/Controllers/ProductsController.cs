using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Controllers;

[ApiController]
[Route("products")] // => URL: /products
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext _db;
    public ProductsController(ProductDbContext db) => _db = db;

    // GET /products
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _db.Products.AsNoTracking().ToListAsync());

    // GET /products/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => await _db.Products.FindAsync(id) is { } p ? Ok(p) : NotFound();

    // POST /products
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product p)
    {
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = p.Id }, p);
    }

    // PUT /products/1   <-- quan trọng: có {id:int}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product p)
    {
        if (id != p.Id) return BadRequest(new { message = "Id mismatch" });
        var exist = await _db.Products.FindAsync(id);
        if (exist is null) return NotFound();

        exist.Name = p.Name;
        exist.Price = p.Price;
        exist.Quantity = p.Quantity;
        exist.Description = p.Description;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /products/1   <-- quan trọng: có {id:int}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
