namespace AI_Driven_Water_Supply.Application.DTOs
{
    public sealed class AdminProfileCountsDto
    {
        public long Consumers { get; init; }
        public long Providers { get; init; }
        public long Banned { get; init; }
        public long ActiveConsumers { get; init; }
    }
}
