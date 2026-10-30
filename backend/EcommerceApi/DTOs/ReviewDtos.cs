using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        [Range(1, 5)] public int Rating { get; set; }
        [MaxLength(1000)] public string Comment { get; set; } = string.Empty;
    }

    public class UpdateReviewDto
    {
        [Range(1, 5)] public int Rating { get; set; }
        [MaxLength(1000)] public string Comment { get; set; } = string.Empty;
    }

    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class ReviewSummaryDto
    {
        public double Average { get; set; }
        public int Count { get; set; }
        public int[] Breakdown { get; set; } = new int[5];     // index 0 = 1 star ... index 4 = 5 stars
    }

    public class ProductReviewsDto
    {
        public ReviewSummaryDto Summary { get; set; } = new();
        public PagedResultDto<ReviewResponseDto> Reviews { get; set; } = new();
        public bool CanReview { get; set; }
        public ReviewResponseDto? MyReview { get; set; }
    }
}