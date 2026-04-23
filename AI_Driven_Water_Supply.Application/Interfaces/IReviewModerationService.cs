namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public sealed record ReviewModerationRequest(
        string Comment,
        int StarRating,
        string ReviewerId,
        string ProviderId,
        string? ConsumerName);

    public enum ReviewModerationDecision
    {
        Accept,
        RejectAbuse,
        RejectSentimentMismatch
    }

    public sealed record ReviewModerationResult(
        ReviewModerationDecision Decision,
        string UserFacingMessage);

    public interface IReviewModerationService
    {
        Task<ReviewModerationResult> EvaluateAsync(
            ReviewModerationRequest request,
            CancellationToken cancellationToken = default);
    }
}
