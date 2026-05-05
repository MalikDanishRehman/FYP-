using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using AI_Driven_Water_Supply.Application.Interfaces;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class SupplierDetails
    {
        private const string ExternalHttpClientName = "ExternalHttp";

        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private IReviewModerationService ReviewModeration { get; set; } = default!;

        [Parameter] public string? name { get; set; }

        private bool isLoading = true;
        private bool isSubmittingReview = false;
        private bool showModal = false;

        private string imageUrl = "/images/fallbackimg.jpg";
        private string description = "";
        private double currentRating = 4.5;
        private string providerId = "";

        private int quantity = 1;
        private int pricePerBottle = 150;
        private int totalPrice => quantity * pricePerBottle;

        private OrderRequestForm orderFormModel = new();
        private List<ReviewModel> reviewsList = new();
        private ReviewModel newReview = new() { Rating = 5 };

        [Table("profiles")]
        public class ProviderDetailProfile : BaseModel
        {
            [Column("id")] public string Id { get; set; } = string.Empty;
            [Column("username")] public string? Username { get; set; }
            [Column("role")] public string? Role { get; set; }
            [Column("rating")] public double Rating { get; set; }
            [Column("profilepic")] public string? ProfilePic { get; set; }
        }

        [Table("profiles")]
        private class ProfileTrustRow : BaseModel
        {
            [Column("id")] public string Id { get; set; } = "";
            [Column("trust_score")] public double? TrustScore { get; set; }
        }

        [Table("reviews")]
        public class ReviewModel : BaseModel
        {
            [Column("id")] public string? Id { get; set; }
            [Column("provider_id")] public string ProviderId { get; set; } = null!;
            [Column("consumer_name")] public string ConsumerName { get; set; } = "Anonymous";
            [Column("rating")] public int Rating { get; set; }
            [Column("comment")] public string Comment { get; set; } = string.Empty;
            [Column("created_at")] public DateTime CreatedAt { get; set; }
            [Column("ip_address")] public string? IpAddress { get; set; }
            [Column("device_fingerprint")] public string? DeviceFingerprint { get; set; }
            [Column("reviewer_id")] public string? ReviewerId { get; set; }
        }

        public class OrderRequestForm
        {
            [Required] public string ConsumerName { get; set; } = "";
            [Required] public string Address { get; set; } = "";
            [Required] public string Phone { get; set; } = "";
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                if (string.IsNullOrEmpty(name)) name = "Unknown";

                description = $"Premium water supply service by {name}. Ensuring clean and timely delivery directly to your doorstep.";

                var user = AuthService.CurrentUser;
                string fetchedName = "Guest";
                if (user != null)
                {
                    if (user.UserMetadata != null && user.UserMetadata.TryGetValue("username", out var nameObj))
                        fetchedName = nameObj?.ToString() ?? "";

                    if (string.IsNullOrEmpty(fetchedName) && !string.IsNullOrEmpty(user.Email))
                        fetchedName = user.Email.Split('@')[0];
                }

                orderFormModel.ConsumerName = fetchedName;
                await LoadProviderData();
            }
            catch (Exception ex) { Console.WriteLine($"Init Error: {ex.Message}"); }
            finally { isLoading = false; }
        }

        private async Task LoadProviderData()
        {
            try
            {
                var response = await _supabase.From<ProviderDetailProfile>()
                    .Where(x => x.Username == name && x.Role == "Provider")
                    .Single();

                if (response != null)
                {
                    providerId = response.Id ?? "";
                    currentRating = response.Rating;
                    if (!string.IsNullOrEmpty(response.ProfilePic))
                    {
                        string? url = _supabase.Storage.From("Avatar").GetPublicUrl(response.ProfilePic);
                        imageUrl = url ?? "/images/fallbackimg.jpg";
                    }

                    await LoadReviews();
                }
            }
            catch (Exception ex) { Console.WriteLine($"Provider Fetch Error: {ex.Message}"); }
        }

        private async Task LoadReviews()
        {
            var reviewRes = await _supabase.From<ReviewModel>()
                .Where(x => x.ProviderId == providerId)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();
            reviewsList = reviewRes.Models;
        }

        private void UpdateQty(int change)
        {
            if (quantity + change >= 1) quantity += change;
        }

        private void OpenOrderModal() => showModal = true;
        private void CloseModal() => showModal = false;

        private string GetStars(double rating) => new string('★', (int)rating).PadRight(5, '☆');

        private async Task HandleOrderSubmit()
        {
            if (string.IsNullOrWhiteSpace(orderFormModel.ConsumerName) ||
                string.IsNullOrWhiteSpace(orderFormModel.Address) ||
                string.IsNullOrWhiteSpace(orderFormModel.Phone))
            {
                ToastService.ShowToast("Missing Information", "Please fill in all delivery details securely.", "warning");
                return;
            }

            try
            {
                var newOrder = new Order
                {
                    ConsumerName = orderFormModel.ConsumerName,
                    Address = orderFormModel.Address,
                    Phone = orderFormModel.Phone,
                    Quantity = quantity,
                    ItemName = "Water Bottle",
                    TotalPrice = totalPrice,
                    SupplierName = name ?? "Unknown",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                var response = await _supabase.From<Order>().Insert(newOrder);
                var insertedOrder = response.Models.FirstOrDefault();

                if (insertedOrder != null)
                {
                    var autoMsg = new Message
                    {
                        OrderId = insertedOrder.Id,
                        SenderName = orderFormModel.ConsumerName,
                        ReceiverName = name ?? "Supplier",
                        Content = $"New Order! {quantity}x Bottles. Address: {orderFormModel.Address}",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _supabase.From<Message>().Insert(autoMsg);

                    showModal = false;
                    ToastService.ShowToast("Order Placed", "Your order has been securely transmitted to the supplier.", "success");
                    Nav.NavigateTo("/Consumer");
                }
                else
                {
                    throw new Exception("Order returned null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Order Error: {ex.Message}");
                ToastService.ShowToast("Order Failed", "System could not process request. Please try again.", "error");
            }
        }

        private async Task SubmitReview()
        {
            var session = _supabase.Auth.CurrentSession;
            if (session == null || session.ExpiresAt() < DateTime.UtcNow)
            {
                await AuthService.TryRefreshSession();
                session = _supabase.Auth.CurrentSession;
            }

            if (session == null || session.ExpiresAt() < DateTime.UtcNow)
            {
                ToastService.ShowToast("Login Required", "Please login to submit a review.", "error");
                return;
            }

            if (string.IsNullOrEmpty(newReview.Comment))
            {
                ToastService.ShowToast("Empty Review", "Please write a comment first.", "warning");
                return;
            }

            if (string.IsNullOrEmpty(providerId)) return;

            var reviewerId = session.User?.Id;
            if (string.IsNullOrEmpty(reviewerId))
            {
                ToastService.ShowToast("Login Required", "Please login to submit a review.", "error");
                return;
            }

            isSubmittingReview = true;
            try
            {
                var supplierName = (name ?? "").Trim();
                var consumerName = (orderFormModel.ConsumerName ?? "").Trim();
                if (!await HasCompletedOrderWithProviderAsync(supplierName, consumerName))
                {
                    ToastService.ShowToast(
                        "Review not allowed",
                        "You can only review after a completed order with this provider.",
                        "error");
                    return;
                }

                var publicIp = await TryGetPublicIpAsync();
                if (string.IsNullOrWhiteSpace(publicIp))
                {
                    ToastService.ShowToast("Network verification failed", "Could not verify network; try again.", "error");
                    return;
                }

                string deviceFingerprint;
                try
                {
                    deviceFingerprint = await JSRuntime.InvokeAsync<string>("getDeviceFingerprint");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fingerprint JSInterop: {ex.Message}");
                    deviceFingerprint = "";
                }

                if (string.IsNullOrWhiteSpace(deviceFingerprint))
                {
                    ToastService.ShowToast("Network verification failed", "Could not verify network; try again.", "error");
                    return;
                }

                var sinceUtc = DateTime.UtcNow.AddHours(-24);
                var existingForProvider = await _supabase.From<ReviewModel>()
                    .Where(x => x.ProviderId == providerId)
                    .Get();
                var recent = existingForProvider.Models.Where(r => r.CreatedAt >= sinceUtc).ToList();

                var sameIpCount = recent.Count(r =>
                    !string.IsNullOrEmpty(r.IpAddress) &&
                    string.Equals(r.IpAddress.Trim(), publicIp.Trim(), StringComparison.OrdinalIgnoreCase));
                var sameFpCount = recent.Count(r =>
                    !string.IsNullOrEmpty(r.DeviceFingerprint) &&
                    string.Equals(r.DeviceFingerprint.Trim(), deviceFingerprint.Trim(), StringComparison.Ordinal));

                if (sameIpCount >= 2 || sameFpCount >= 2)
                {
                    ToastService.ShowToast("Suspicious activity detected", "Suspicious activity detected", "error");
                    return;
                }

                newReview.Id = Guid.NewGuid().ToString();
                newReview.ProviderId = providerId;
                newReview.ConsumerName = string.IsNullOrEmpty(consumerName) ? "Anonymous" : consumerName;
                newReview.CreatedAt = DateTime.UtcNow;
                newReview.IpAddress = publicIp.Trim();
                newReview.DeviceFingerprint = deviceFingerprint.Trim();
                newReview.ReviewerId = reviewerId;

                var moderation = await ReviewModeration.EvaluateAsync(new ReviewModerationRequest(
                    newReview.Comment,
                    newReview.Rating,
                    reviewerId,
                    providerId,
                    string.IsNullOrEmpty(consumerName) ? null : consumerName));

                if (moderation.Decision != ReviewModerationDecision.Accept)
                {
                    ToastService.ShowToast("Review not posted", moderation.UserFacingMessage, "warning");
                    return;
                }

                await _supabase.From<ReviewModel>().Insert(newReview);

                var response = await _supabase.From<ReviewModel>()
                    .Where(x => x.ProviderId == providerId)
                    .Get();

                var allReviews = response.Models;
                var trustMap = await LoadReviewerTrustScoresAsync(allReviews.Select(r => r.ReviewerId));
                var newAverageRating = ComputeWeightedRating(allReviews, trustMap);

                await _supabase.From<ProviderDetailProfile>()
                    .Where(x => x.Id == providerId)
                    .Set(x => x.Rating, newAverageRating)
                    .Update();

                currentRating = newAverageRating;
                reviewsList = allReviews.OrderByDescending(x => x.CreatedAt).ToList();

                ToastService.ShowToast("Review Submitted", "Thank you! Your feedback helps others.", "success");

                newReview = new ReviewModel { Rating = 5, Comment = string.Empty };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Review/Update Error: " + ex.Message);
                ToastService.ShowToast("Error", "Something went wrong. Please try again.", "error");
            }
            finally { isSubmittingReview = false; }
        }

        private async Task<bool> HasCompletedOrderWithProviderAsync(string supplierName, string consumerName)
        {
            if (string.IsNullOrEmpty(supplierName) || string.IsNullOrEmpty(consumerName))
                return false;

            // Chained Where (not a single lambda with &&): PostgREST otherwise can emit invalid logic like "and.(...)",
            // causing PGRST100 "failed to parse logic tree".
            var res = await _supabase.From<Order>()
                .Where(x => x.SupplierName == supplierName)
                .Where(x => x.ConsumerName == consumerName)
                .Where(x => x.Status == "Completed")
                .Limit(1)
                .Get();

            return res.Models.Count > 0;
        }

        private async Task<string?> TryGetPublicIpAsync()
        {
            try
            {
                var client = HttpClientFactory.CreateClient(ExternalHttpClientName);
                using var response = await client.GetAsync("https://api.ipify.org?format=json");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ip", out var ipEl))
                {
                    var ip = ipEl.GetString();
                    return string.IsNullOrWhiteSpace(ip) ? null : ip;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ipify error: {ex.Message}");
            }

            return null;
        }

        private async Task<IReadOnlyDictionary<string, double>> LoadReviewerTrustScoresAsync(IEnumerable<string?> reviewerIds)
        {
            var distinct = reviewerIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (distinct.Count == 0)
                return new Dictionary<string, double>(StringComparer.Ordinal);

            var tasks = distinct.Select(async id =>
            {
                var res = await _supabase.From<ProfileTrustRow>().Where(x => x.Id == id).Get();
                var m = res.Models.FirstOrDefault();
                var w = m?.TrustScore is double td && td > 0 ? td : 1.0;
                return (id, w);
            });

            var pairs = await Task.WhenAll(tasks);
            return pairs.ToDictionary(p => p.id, p => p.w, StringComparer.Ordinal);
        }

        private static double ComputeWeightedRating(IReadOnlyList<ReviewModel> reviews, IReadOnlyDictionary<string, double> trustByReviewerId)
        {
            double sumW = 0;
            double sumRW = 0;

            foreach (var r in reviews)
            {
                double w = 1.0;
                if (!string.IsNullOrEmpty(r.ReviewerId) &&
                    trustByReviewerId.TryGetValue(r.ReviewerId, out var t) &&
                    t > 0)
                {
                    w = t;
                }

                sumRW += r.Rating * w;
                sumW += w;
            }

            if (sumW <= 0)
                return reviews.Count == 0 ? 0 : reviews.Average(x => x.Rating);

            return sumRW / sumW;
        }
    }
}
