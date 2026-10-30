using System.Security.Claims;
using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReviewController(AppDbContext db) => _db = db;

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // True if this user has a PAID order containing this product
        private Task<bool> HasPurchasedAsync(int userId, int productId) =>
            _db.OrderItems.AnyAsync(oi => oi.ProductId == productId &&
                _db.Orders.Any(o => o.Id == oi.OrderId && o.UserId == userId && o.Status == "Paid"));

        // GET /api/review/product/5?pageNumber=1&pageSize=5
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetForProduct(int productId,
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 20);
            var userId = CurrentUserId;

            if (!await _db.Products.AnyAsync(p => p.Id == productId))
                return NotFound(new { message = "Product not found." });

            var query = _db.Reviews.Where(r => r.ProductId == productId);

            // one grouped query gives the count, the average and the 1-5 star breakdown
            var counts = await query.GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            var breakdown = new int[5];
            foreach (var c in counts) breakdown[c.Rating - 1] = c.Count;
            var total = breakdown.Sum();
            var average = total == 0 ? 0 : Math.Round(counts.Sum(c => c.Rating * c.Count) / (double)total, 1);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    UserName = r.User!.FullName,
                    CreatedAt = r.CreatedAt,
                    IsMine = r.UserId == userId
                })
                .ToListAsync();

            var mine = await query.Where(r => r.UserId == userId)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    UserName = r.User!.FullName,
                    CreatedAt = r.CreatedAt,
                    IsMine = true
                })
                .FirstOrDefaultAsync();

            var canReview = mine == null && await HasPurchasedAsync(userId, productId);

            return Ok(new ProductReviewsDto
            {
                Summary = new ReviewSummaryDto { Average = average, Count = total, Breakdown = breakdown },
                Reviews = new PagedResultDto<ReviewResponseDto>
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                },
                CanReview = canReview,
                MyReview = mine
            });
        }

        // POST /api/review
        [HttpPost]
        public async Task<IActionResult> Create(CreateReviewDto dto)
        {
            var userId = CurrentUserId;

            if (!await _db.Products.AnyAsync(p => p.Id == dto.ProductId))
                return NotFound(new { message = "Product not found." });

            if (!await HasPurchasedAsync(userId, dto.ProductId))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "You can only review products you have purchased." });

            if (await _db.Reviews.AnyAsync(r => r.ProductId == dto.ProductId && r.UserId == userId))
                return Conflict(new { message = "You have already reviewed this product." });

            var review = new Review
            {
                ProductId = dto.ProductId,
                UserId = userId,                      // from the token, never from the body
                Rating = dto.Rating,
                Comment = dto.Comment.Trim()
            };
            _db.Reviews.Add(review);

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException)                 // two quick clicks: the unique index catches the second
            {
                return Conflict(new { message = "You have already reviewed this product." });
            }

            return Ok(new { message = "Review added.", id = review.Id });
        }

        // PUT /api/review/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateReviewDto dto)
        {
            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return NotFound(new { message = "Review not found." });
            if (review.UserId != CurrentUserId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only edit your own review." });

            review.Rating = dto.Rating;
            review.Comment = dto.Comment.Trim();
            review.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Review updated." });
        }

        // DELETE /api/review/5   (the owner, or an Admin moderating)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return NotFound(new { message = "Review not found." });
            if (review.UserId != CurrentUserId && !User.IsInRole("Admin"))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Not allowed." });

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Review deleted." });
        }
    }
}