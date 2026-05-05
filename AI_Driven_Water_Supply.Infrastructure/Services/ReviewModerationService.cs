using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AI_Driven_Water_Supply.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    /// <summary>
    /// Pre-insert review moderation using Google Gemini (<c>GEMINI_API_KEY</c> from environment / .env via <see cref="IConfiguration"/>).
    /// </summary>
    public class ReviewModerationService : IReviewModerationService
    {
        public const string AlertTypeReviewAbuse = "review_abuse";
        public const string AlertTypeReviewSentimentMismatch = "review_sentiment_mismatch";

        private const string GeminiModel = "gemini-2.0-flash";
        private const string GeminiApiKeyConfigKey = "GEMINI_API_KEY";

        private readonly Supabase.Client _supabase;
        private readonly ILogger<ReviewModerationService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ReviewModerationService(
            Supabase.Client supabase,
            ILogger<ReviewModerationService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _supabase = supabase;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<ReviewModerationResult> EvaluateAsync(
            ReviewModerationRequest request,
            CancellationToken cancellationToken = default)
        {
            var comment = request.Comment ?? string.Empty;

            var heuristicAbuse = DetectAbuseHeuristic(comment);
            var (geminiAbuse, sentiment) = await ModerateWithGeminiAsync(comment, cancellationToken).ConfigureAwait(false);
            var abuse = geminiAbuse || heuristicAbuse;

            if (heuristicAbuse && !geminiAbuse)
            {
                _logger.LogInformation(
                    "ReviewModeration: heuristic abuse flag fired (Gemini did not flag). Comment length={Length}",
                    comment.Length);
            }

            if (abuse)
            {
                await SendWarningEmailAsync(request.ReviewerId, cancellationToken).ConfigureAwait(false);
                await RecordAdminAlertAsync(
                    AlertTypeReviewAbuse,
                    "Review blocked: profanity or abuse detected (model and/or heuristic check).",
                    BuildDetail(request, comment),
                    cancellationToken).ConfigureAwait(false);

                return new ReviewModerationResult(
                    ReviewModerationDecision.RejectAbuse,
                    "Your review was not posted because the comment contains language that is not allowed.");
            }

            if (IsSentimentStarMismatch(request.StarRating, sentiment))
            {
                await RecordAdminAlertAsync(
                    AlertTypeReviewSentimentMismatch,
                    "Review blocked: star rating does not match comment sentiment (Gemini).",
                    BuildDetail(request, comment, sentiment.ToString()),
                    cancellationToken).ConfigureAwait(false);

                return new ReviewModerationResult(
                    ReviewModerationDecision.RejectSentimentMismatch,
                    "Your review was not posted because the star rating does not match the tone of your comment.");
            }

            return new ReviewModerationResult(
                ReviewModerationDecision.Accept,
                string.Empty);
        }

        /// <summary>
        /// Calls Gemini once for abuse detection and sentiment. If <c>GEMINI_API_KEY</c> is missing or the call fails, returns no abuse and neutral sentiment (moderation effectively skipped).
        /// </summary>
        private async Task<(bool abuse, SentimentLabel sentiment)> ModerateWithGeminiAsync(
            string comment,
            CancellationToken cancellationToken)
        {
            var apiKey = _configuration[GeminiApiKeyConfigKey]
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning(
                    "ReviewModeration: GEMINI_API_KEY is not set; skipping Gemini moderation (comment accepted unless other checks apply).");
                return (false, SentimentLabel.Neutral);
            }

            var model = _configuration["GEMINI_MODEL"]?.Trim();
            if (string.IsNullOrEmpty(model))
                model = GeminiModel;

            var prompt =
                "You moderate short customer reviews for a water delivery marketplace.\n\n" +
                "Rules:\n" +
                "1. abuse MUST be true if the comment contains ANY profanity or insult targeting someone (examples: variants of \"f***\", \"f you\", vulgar insults, slurs, threats, harassment, hate, graphic sexual language). Casual jokes WITHOUT profanity or insult can be abuse false.\n" +
                "2. sentiment: how the customer describes their EXPERIENCE with the SERVICE — \"positive\" (praising or satisfied), \"negative\" (complaints about service), or \"neutral\" (factual, mixed, or mild).\n\n" +
                "Output ONLY valid JSON object with lowercase keys \"abuse\" (boolean true/false only) and \"sentiment\" (string, exactly one of: positive, negative, neutral). No markdown, no code fences, no extra keys.\n\n" +
                "Comment: " + JsonSerializer.Serialize(comment);

            var requestUri =
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey.Trim())}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.05,
                    responseMimeType = "application/json"
                }
            };

            try
            {
                var client = _httpClientFactory.CreateClient("Gemini");
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var jsonBody = JsonSerializer.Serialize(payload, jsonOptions);
                using var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(requestUri, httpContent, cancellationToken)
                    .ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "ReviewModeration: Gemini HTTP {Status}. Body: {Body}",
                        (int)response.StatusCode,
                        body.Length > 500 ? body[..500] : body);
                    return (false, SentimentLabel.Neutral);
                }

                using var doc = JsonDocument.Parse(body);
                var text = ExtractGeminiResponseText(doc.RootElement);
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("ReviewModeration: Gemini returned empty text.");
                    return (false, SentimentLabel.Neutral);
                }

                text = StripMarkdownJsonFence(text.Trim());
                var jsonStart = text.IndexOf('{');
                var jsonEnd = text.LastIndexOf('}');
                if (jsonStart < 0 || jsonEnd <= jsonStart)
                {
                    _logger.LogWarning("ReviewModeration: Gemini text is not JSON: {Text}", text.Length > 200 ? text[..200] : text);
                    return (false, SentimentLabel.Neutral);
                }

                text = text[jsonStart..(jsonEnd + 1)];
                using var resultDoc = JsonDocument.Parse(text);
                var root = resultDoc.RootElement;

                var abuse = ResolveAbuseFromRoot(root);
                var sentiment = ResolveSentimentFromRoot(root);

                return (abuse, sentiment);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ReviewModeration: Gemini call failed; skipping moderation for this request.");
                return (false, SentimentLabel.Neutral);
            }
        }

        /// <summary>
        /// Deterministic coarse filter when Gemini returns false negatives or skips classification.
        /// </summary>
        private static bool DetectAbuseHeuristic(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment)) return false;

            var lower = comment.ToLowerInvariant();

            // Word-boundary style profanity / common insult stems (English).
            if (ProfanityWordBoundaryRegex.IsMatch(lower))
                return true;

            var compactLetters = LettersOnlyRegex.Replace(lower, "");
            foreach (var stem in ProfanityStems)
            {
                if (compactLetters.Contains(stem, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static readonly Regex LettersOnlyRegex = new(@"[^a-z]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ProfanityWordBoundaryRegex = new(
            @"\b(f+[\s\*\.\-_']*u+[\s\*\.\-_']*c+[\s\*\.\-_']*k+|s+h+[\s\*\.\-_']*i+[\s\*\.\-_']*t+|b+i+t+c+h+|a+s+s+h+o+l+e+|c+u+n+t+|d+i+c+k+|p+i+s+s+|w+h+o+r+e+)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly string[] ProfanityStems =
        {
            "fuckyou", "fuck", "motherfucker", "bullshit", "bitch"
        };

        private static bool ResolveAbuseFromRoot(JsonElement root)
        {
            if (ParseAbuseBool(root) == true)
                return true;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in root.EnumerateObject())
                {
                    if (p.Value.ValueKind != JsonValueKind.Object)
                        continue;
                    if (ParseAbuseBool(p.Value) == true)
                        return true;
                }
            }

            return false;
        }

        /// <returns>true / false when present; null if no recognizable abuse flag.</returns>
        private static bool? ParseAbuseBool(JsonElement root)
        {
            if (!TryGetPropertyIgnoreCase(root, "abuse", out var abuseEl))
                return null;
            return ParseLooseBool(abuseEl);
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
        {
            value = default;
            if (obj.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var p in obj.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool? ParseLooseBool(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.String:
                    var s = el.GetString()?.Trim();
                    if (string.IsNullOrEmpty(s))
                        return null;
                    if (bool.TryParse(s, out var b))
                        return b;
                    if (string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase)) return true;
                    if (string.Equals(s, "no", StringComparison.OrdinalIgnoreCase)) return false;
                    if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                        return n != 0;
                    return null;
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out var num))
                        return num != 0;
                    return null;
                default:
                    return null;
            }
        }

        private static SentimentLabel ResolveSentimentFromRoot(JsonElement root)
        {
            if (TryParseSentiment(root, out var s))
                return s;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in root.EnumerateObject())
                {
                    if (p.Value.ValueKind != JsonValueKind.Object)
                        continue;
                    if (TryParseSentiment(p.Value, out var inner))
                        return inner;
                }
            }

            return SentimentLabel.Neutral;
        }

        private static bool TryParseSentiment(JsonElement root, out SentimentLabel sentiment)
        {
            sentiment = SentimentLabel.Neutral;
            if (!TryGetPropertyIgnoreCase(root, "sentiment", out var sentEl) ||
                sentEl.ValueKind != JsonValueKind.String)
                return false;

            sentiment = sentEl.GetString()?.ToLowerInvariant() switch
            {
                "positive" => SentimentLabel.Positive,
                "negative" => SentimentLabel.Negative,
                _ => SentimentLabel.Neutral
            };
            return true;
        }

        private static string ExtractGeminiResponseText(JsonElement root)
        {
            if (!root.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
                return string.Empty;

            var first = candidates[0];
            if (!first.TryGetProperty("content", out var content))
                return string.Empty;

            if (!content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                return string.Empty;

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    return textEl.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string StripMarkdownJsonFence(string text)
        {
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                text = text["```json".Length..].TrimStart();
            else if (text.StartsWith("```", StringComparison.Ordinal))
                text = text[3..].TrimStart();

            if (text.EndsWith("```", StringComparison.Ordinal))
                text = text[..^3].TrimEnd();

            return text.Trim();
        }

        /// <summary>
        /// Wire to email (e.g. Supabase Edge Function with service role, or SMTP). Anon key cannot read auth.users email by reviewer id alone.
        /// </summary>
        protected virtual Task SendWarningEmailAsync(string reviewerId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "ReviewModeration: SendWarningEmailAsync placeholder for reviewer_id={ReviewerId}",
                reviewerId);
            return Task.CompletedTask;
        }

        private static bool IsSentimentStarMismatch(int starRating, SentimentLabel sentiment)
        {
            if (starRating is 1 or 2 && sentiment == SentimentLabel.Positive)
                return true;
            if (starRating is 4 or 5 && sentiment == SentimentLabel.Negative)
                return true;
            return false;
        }

        private static string BuildDetail(ReviewModerationRequest request, string comment, string? sentiment = null)
        {
            var excerpt = comment.Length <= 400 ? comment : comment[..400] + "…";
            return JsonSerializer.Serialize(new
            {
                request.ReviewerId,
                request.ProviderId,
                request.ConsumerName,
                request.StarRating,
                comment_excerpt = excerpt,
                sentiment
            });
        }

        private async Task RecordAdminAlertAsync(
            string alertType,
            string message,
            string detailJson,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _supabase.Auth.CurrentUser?.Id;
                var hasSession = _supabase.Auth.CurrentSession != null;
                _logger.LogInformation(
                    "ReviewModeration: writing admin alert type={AlertType}, hasSession={HasSession}, userId={UserId}",
                    alertType,
                    hasSession,
                    string.IsNullOrWhiteSpace(currentUserId) ? "<none>" : currentUserId);

                var row = new AdminAlertRow
                {
                    AlertType = alertType,
                    Message = message,
                    Detail = detailJson,
                    Read = false
                };

                // Request minimal response so reviewer inserts do not require SELECT on admin_alerts.
                await _supabase.From<AdminAlertRow>().Insert(
                        row,
                        new QueryOptions { Returning = QueryOptions.ReturnType.Minimal },
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "ReviewModeration: failed to insert admin_alerts row type={AlertType}. Error={Error}. Falling back to log only.",
                    alertType,
                    ex.Message);
                _logger.LogInformation(
                    "ReviewModeration admin alert (fallback): {AlertType} | {Message} | {Detail}",
                    alertType,
                    message,
                    detailJson);
            }
        }

        private enum SentimentLabel
        {
            Positive,
            Negative,
            Neutral
        }

        [Table("admin_alerts")]
        private sealed class AdminAlertRow : BaseModel
        {
            [Column("alert_type")] public string AlertType { get; set; } = "";

            [Column("message")] public string Message { get; set; } = "";

            [Column("detail")] public string Detail { get; set; } = "{}";

            [Column("read")] public bool Read { get; set; }
        }
    }
}
