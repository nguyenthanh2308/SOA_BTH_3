namespace ProductService.DTOs
{
    public class StockAdjustItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }   // phải > 0
    }

    public class StockAdjustRequest
    {
        public List<StockAdjustItemDto> Items { get; set; } = new();
    }
}
