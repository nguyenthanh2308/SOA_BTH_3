public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(
        System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Computed)]
    public decimal TotalPrice { get; private set; }        // computed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }              // ✅ cho phép null
}