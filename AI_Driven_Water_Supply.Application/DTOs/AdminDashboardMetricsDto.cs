namespace AI_Driven_Water_Supply.Application.DTOs
{
    public sealed class AdminDashboardMetricsDto
    {
        public long TotalRevenuePkr { get; init; }
        public long ActiveVendors { get; init; }
        public decimal SuccessRatePercent { get; init; }
        public long PendingDisputes { get; init; }
        public long PendingOrders { get; init; }
    }
}
