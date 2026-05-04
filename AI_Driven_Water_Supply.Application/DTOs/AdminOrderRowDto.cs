namespace AI_Driven_Water_Supply.Application.DTOs
{
    public sealed class AdminOrderRowDto
    {
        public long Id { get; init; }
        public string ConsumerName { get; init; } = "";
        public string SupplierName { get; init; } = "";
        public int TotalPrice { get; init; }
        public string Status { get; init; } = "";
    }
}
