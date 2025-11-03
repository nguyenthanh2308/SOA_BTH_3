public record OrderItemCreateDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
public record OrderCreateDto(string CustomerName, string CustomerEmail, List<OrderItemCreateDto> Items);
public record OrderUpdateStatusDto(string Status); // pending/completed/cancelled
