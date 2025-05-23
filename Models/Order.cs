namespace webprogbackend.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
