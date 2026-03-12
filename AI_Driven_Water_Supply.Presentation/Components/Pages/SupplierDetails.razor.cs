using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using AI_Driven_Water_Supply.Application.Interfaces;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class SupplierDetails
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;

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

        [Table("reviews")]
        public class ReviewModel : BaseModel
        {
            [Column("id")] public string? Id { get; set; }
            [Column("provider_id")] public string ProviderId { get; set; } = null!;
            [Column("consumer_name")] public string ConsumerName { get; set; } = "Anonymous";
            [Column("rating")] public int Rating { get; set; }
            [Column("comment")] public string Comment { get; set; } = string.Empty;
            [Column("created_at")] public DateTime CreatedAt { get; set; }
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
                ToastService.ShowToast("Login Required", "Please login to submit a review.", "error");
                return;
            }

            if (string.IsNullOrEmpty(newReview.Comment))
            {
                ToastService.ShowToast("Empty Review", "Please write a comment first.", "warning");
                return;
            }

            if (string.IsNullOrEmpty(providerId)) return;

            isSubmittingReview = true;
            try
            {
                newReview.Id = Guid.NewGuid().ToString();
                newReview.ProviderId = providerId;
                newReview.ConsumerName = orderFormModel.ConsumerName ?? "Anonymous";
                newReview.CreatedAt = DateTime.UtcNow;

                await _supabase.From<ReviewModel>().Insert(newReview);

                var response = await _supabase.From<ReviewModel>()
                    .Where(x => x.ProviderId == providerId)
                    .Get();

                var allReviews = response.Models;

                double newAverageRating = 0;
                if (allReviews.Count > 0)
                {
                    newAverageRating = allReviews.Average(r => r.Rating);
                }

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
    }
}
