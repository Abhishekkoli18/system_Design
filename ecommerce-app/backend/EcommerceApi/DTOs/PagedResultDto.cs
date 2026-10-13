namespace EcommerceApi.DTOs
{
    // A generic "wrapper" around any list - carries the page of items
    // PLUS metadata about the full result set (total count, total pages).
    // <T> means this works for a list of ANY type, not just products.
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}