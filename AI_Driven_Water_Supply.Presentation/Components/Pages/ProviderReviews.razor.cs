using Microsoft.AspNetCore.Components;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using AI_Driven_Water_Supply.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class ProviderReviews
    {
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private bool isLoading = true;
        private double averageRating = 0;
        private int totalReviews = 0;
        private Dictionary<int, int> starCounts = new();
        private List<ReviewModel> reviewsList = new();

        [Table("reviews")]
        public class ReviewModel : BaseModel
        {
            [Column("id")] public string Id { get; set; } = null!;
            [Column("provider_id")] public string ProviderId { get; set; } = "";
            [Column("comment")] public string Comment { get; set; } = "";
            [Column("rating")] public int Rating { get; set; }
            [Column("user_name")] public string UserName { get; set; } = "";
            [Column("created_at")] public DateTime CreatedAt { get; set; }
        }

        [Table("profiles")]
        public class ProfileModel : BaseModel
        {
            [Column("id")] public string Id { get; set; } = null!;
            [Column("rating")] public double Rating { get; set; }
        }

        protected override async Task OnInitializedAsync()
        {
            bool shouldRedirect = false;

            try
            {
                isLoading = true;

                var user = AuthService.CurrentUser;
                if (user == null)
                {
                    await AuthService.TryRefreshSession();
                    user = AuthService.CurrentUser;
                }

                if (user == null)
                {
                    shouldRedirect = true;
                }
                else
                {
                    var profileResponse = await _supabase
                        .From<ProfileModel>()
                        .Where(p => p.Id == user.Id)
                        .Get();

                    var profile = profileResponse.Models.FirstOrDefault();
                    if (profile != null)
                    {
                        averageRating = profile.Rating;
                    }

                    var response = await _supabase
                        .From<ReviewModel>()
                        .Where(x => x.ProviderId == user.Id)
                        .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                        .Get();

                    reviewsList = response.Models;
                    totalReviews = reviewsList.Count;

                    if (totalReviews > 0)
                    {
                        starCounts = reviewsList
                            .GroupBy(r => r.Rating)
                            .ToDictionary(g => g.Key, g => g.Count());

                        if (averageRating == 0)
                        {
                            averageRating = reviewsList.Average(r => r.Rating);
                        }
                    }
                    else
                    {
                        starCounts = new Dictionary<int, int>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }

            if (shouldRedirect)
            {
                Nav.NavigateTo("/login");
            }
        }

        private string GetStarHtml(double rating)
        {
            int fullStars = (int)rating;
            bool halfStar = (rating - fullStars) >= 0.5;
            string html = "";
            for (int i = 0; i < fullStars; i++) html += "<i class='fa-solid fa-star'></i> ";
            if (halfStar) html += "<i class='fa-solid fa-star-half-stroke'></i> ";
            int filledCount = fullStars + (halfStar ? 1 : 0);
            for (int i = 0; i < (5 - filledCount); i++) html += "<i class='fa-regular fa-star'></i> ";
            return html;
        }
    }
}
