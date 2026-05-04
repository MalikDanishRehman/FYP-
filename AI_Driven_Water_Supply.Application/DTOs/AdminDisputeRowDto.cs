using System;

namespace AI_Driven_Water_Supply.Application.DTOs
{
    public sealed class AdminDisputeRowDto
    {
        public Guid Id { get; init; }
        public string? ConsumerName { get; init; }
        public string? ProviderName { get; init; }
        public string IssueType { get; init; } = "";
        public string Description { get; init; } = "";
        public string Priority { get; init; } = "";
        public string Status { get; init; } = "";
        public DateTime CreatedAt { get; init; }
    }
}
