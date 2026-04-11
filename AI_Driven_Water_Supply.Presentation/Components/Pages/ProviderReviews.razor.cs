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
            [Column("consumer_name")] public string ConsumerName { get; set; } = "";
            [Column("reviewer_id")] public string? ReviewerId { get; set; }
            [Column("created_at")] public DateTime CreatedAt { get; set; }
        }

        [Table("profiles")]
        public class ProfileModel : BaseModel
        {
            [Column("id")] public string Id { get; set; } = null!;
            [Column("rating")] public double Rating { get; set; }
        }

        [Table("profiles")]
        private class ProfileTrustRow : BaseModel
        {
            [Column("id")] public string Id { get; set; } = "";
            [Column("trust_score")] public double? TrustScore { get; set; }
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
                            var trustMap = await LoadReviewerTrustScoresAsync(reviewsList.Select(r => r.ReviewerId));
                            averageRating = ComputeWeightedRating(reviewsList, trustMap);
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

        private async Task<Dictionary<string, double>> LoadReviewerTrustScoresAsync(IEnumerable<string?> reviewerIds)
        {
            var distinct = reviewerIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var dict = new Dictionary<string, double>(StringComparer.Ordinal);
            if (distinct.Count == 0)
                return dict;

            var tasks = distinct.Select(async id =>
            {
                var res = await _supabase.From<ProfileTrustRow>().Where(x => x.Id == id).Get();
                var m = res.Models.FirstOrDefault();
                var w = m?.TrustScore is double td && td > 0 ? td : 1.0;
                return (id, w);
            });

            foreach (var pair in await Task.WhenAll(tasks))
                dict[pair.id] = pair.w;

            return dict;
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
