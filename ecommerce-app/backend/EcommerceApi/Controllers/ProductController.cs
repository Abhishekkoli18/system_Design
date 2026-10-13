using System.Globalization;
using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]   // becomes /api/product
    [Authorize]                   // must be logged in for everything in this controller
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ProductController> _logger;

        private readonly IDistributedCache _cache;

        public ProductController(AppDbContext db, ILogger<ProductController> logger, IDistributedCache cache)
        {
            _db = db;
            _logger = logger;
            _cache = cache;
        }
        // public ProductController(AppDbContext db, IDistributedCache cache)
        // {
        //     _db = db;
        //     _cache = cache;
        // }

        // GET /api/product
        // GET /api/product?search=keyboard
        // GET /api/product?minPrice=500&maxPrice=2000
        // GET /api/product?category=Electronics
        // GET /api/product?inStockOnly=true
        // All filters are optional and can be combined. Any logged-in user can view.
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? category,
            [FromQuery] bool? inStockOnly,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _db.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            if (inStockOnly == true)
                query = query.Where(p => p.Quantity > 0);

            // Count ALL matching rows first, BEFORE Skip/Take are applied -
            // this is the "how many pages exist in total" number.
            var totalCount = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Category = p.Category,
                    Price = p.Price,
                    Quantity = p.Quantity
                })
                .ToListAsync();

            var result = new PagedResultDto<ProductResponseDto>
            {
                Items = products,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(result);
        }


        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var cached = await _cache.GetStringAsync("categories");
            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<List<string>>(cached));
            }

            var categories = await _db.Products
                .Where(p => p.Category != null && p.Category != "")
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            await _cache.SetStringAsync(
                "categories",
                JsonSerializer.Serialize(categories),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }
            );

            return Ok(categories);
        }   

        // POST /api/product/add
        // Manual single-product add. Admin only.
        [HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(ProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                Category = dto.Category,
                Price = dto.Price,
                Quantity = dto.Quantity
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Product added: {Name}, Price: {Price}", product.Name, product.Price);
            await _cache.RemoveAsync("categories");

            return Ok(new { message = "Product added successfully.", product.Id });
        }

        // DELETE /api/product/5
        // Delete a single product. Admin only.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            await _cache.RemoveAsync("categories");

            return Ok(new { message = "Product deleted." });
        }

        // DELETE /api/product/delete-all
        // Clears every product - useful for cleaning up test/demo data.
        // Admin only.
        [HttpDelete("delete-all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAll()
        {
            var all = await _db.Products.ToListAsync();
            _db.Products.RemoveRange(all);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Deleted {all.Count} product(s)." });
        }

        // PUT /api/product/5
        // Edit an existing product. Admin only.
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, ProductDto dto)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.ImageUrl = dto.ImageUrl;
            product.Category = dto.Category;
            product.Price = dto.Price;
            product.Quantity = dto.Quantity;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Product {Id} updated: {Name}", id, product.Name);
            await _cache.RemoveAsync("categories");

            return Ok(new { message = "Product updated successfully." });
        }

        // POST /api/product/upload-csv
        // Bulk add via CSV file. Admin only.
        // Expected CSV format (with header row):
        // Name,Price,Quantity,Description,ImageUrl,Category
        // Keyboard,999.00,50,Mechanical keyboard,https://example.com/keyboard.jpg,Electronics
        // (Description, ImageUrl, and Category are all optional columns.)
        [HttpPost("upload-csv")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Please upload a .csv file." });

            var addedProducts = new List<Product>();
            var errors = new List<string>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                string? line;
                int lineNumber = 0;
                bool isFirstLine = true;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;

                    // Skip the header row
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                        continue; // skip blank lines

                    var columns = line.Split(',');

                    if (columns.Length < 3)
                    {
                        errors.Add($"Line {lineNumber}: expected at least 3 columns (Name,Price,Quantity), got {columns.Length}");
                        continue;
                    }

                    var name = columns[0].Trim();
                    var priceText = columns[1].Trim();
                    var quantityText = columns[2].Trim();
                    var description = columns.Length > 3 ? columns[3].Trim() : string.Empty;
                    var imageUrl = columns.Length > 4 ? columns[4].Trim() : string.Empty;
                    var category = columns.Length > 5 ? columns[5].Trim() : string.Empty;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add($"Line {lineNumber}: product name is empty");
                        continue;
                    }

                    if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    {
                        errors.Add($"Line {lineNumber}: '{priceText}' is not a valid price");
                        continue;
                    }

                    if (!int.TryParse(quantityText, out var quantity))
                    {
                        errors.Add($"Line {lineNumber}: '{quantityText}' is not a valid quantity");
                        continue;
                    }

                    addedProducts.Add(new Product
                    {
                        Name = name,
                        Price = price,
                        Quantity = quantity,
                        Description = description,
                        ImageUrl = imageUrl,
                        Category = category
                    });
                }
            }

            if (addedProducts.Count > 0)
            {
                _db.Products.AddRange(addedProducts);
                await _db.SaveChangesAsync();
                await _cache.RemoveAsync("categories");
            }
            _logger.LogInformation("CSV upload: {AddedCount} added, {ErrorCount} errors", addedProducts.Count, errors.Count);
            return Ok(new
            {
                message = $"{addedProducts.Count} product(s) added.",
                addedCount = addedProducts.Count,
                errorCount = errors.Count,
                errors
            });
        }
    }
}
