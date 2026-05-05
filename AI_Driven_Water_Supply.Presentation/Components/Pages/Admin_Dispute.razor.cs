using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Admin_Dispute
    {
        [Inject] private IAdminDisputeService Disputes { get; set; } = default!;
        [Inject] private IAdminDashboardService Dashboard { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private Supabase.Client SupabaseClient { get; set; } = default!;

        private string activeTab = "All";
        private bool showModal;
        private AdminDisputeRowDto? selectedTicket;
        private string resolutionDraft = "";
        private bool isLoading = true;
        private bool isSubmitting;
        private List<AdminDisputeRowDto> tickets = new();
        private long pendingFromMetrics;

        private IEnumerable<AdminDisputeRowDto> FilteredTickets
        {
            get
            {
                if (activeTab == "All") return tickets;
                if (activeTab == "Pending") return tickets.Where(t => t.Status == "Pending");
                return tickets.Where(t => t.Status is "Resolved" or "Closed");
            }
        }

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null)
            {
                await AuthService.TryRefreshSession();
                user = AuthService.CurrentUser;
            }

            if (user == null)
            {
                Nav.NavigateTo("/login");
                return;
            }

            await ReloadAsync();
        }

        protected async Task ReloadAsync()
        {
            isLoading = true;
            StateHasChanged();

            var tab = activeTab == "All" ? null : activeTab;
            var page = await Disputes.ListAsync(tab, 1, 100, default);
            var disputeTickets = page.Items.ToList();
            var abusiveTickets = await LoadAbusiveContentTicketsAsync();
            tickets = disputeTickets
                .Concat(abusiveTickets)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            var metrics = await Dashboard.GetMetricsAsync();
            pendingFromMetrics = metrics?.PendingDisputes ?? tickets.Count(t => t.Status == "Pending");

            isLoading = false;
            StateHasChanged();
        }

        private async Task<List<AdminDisputeRowDto>> LoadAbusiveContentTicketsAsync()
        {
            try
            {
                var response = await SupabaseClient.From<AdminAlert>()
                    .Where(x => x.AlertType == "review_abuse")
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(100)
                    .Get();

                return response.Models.Select(alert =>
                {
                    var detail = ParseAlertDetail(alert.Detail);
                    var excerpt = string.IsNullOrWhiteSpace(detail.CommentExcerpt)
                        ? "Flagged by moderation agent as abusive language."
                        : detail.CommentExcerpt!;

                    return new AdminDisputeRowDto
                    {
                        Id = alert.Id,
                        ConsumerName = detail.ConsumerName ?? ShortId(detail.ReviewerId),
                        ProviderName = ShortId(detail.ProviderId),
                        IssueType = "Abusive Content",
                        Description = excerpt,
                        Priority = "High",
                        Status = alert.Read ? "Resolved" : "Pending",
                        CreatedAt = alert.CreatedAt
                    };
                }).ToList();
            }
            catch
            {
                return new List<AdminDisputeRowDto>();
            }
        }

        private static string? ShortId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return id.Length > 8 ? id[..8] + "..." : id;
        }

        private static AlertDetail ParseAlertDetail(string? detailJson)
        {
            if (string.IsNullOrWhiteSpace(detailJson)) return new AlertDetail();

            try
            {
                using var doc = JsonDocument.Parse(detailJson);
                var root = doc.RootElement;
                return new AlertDetail
                {
                    ReviewerId = GetString(root, "ReviewerId"),
                    ProviderId = GetString(root, "ProviderId"),
                    ConsumerName = GetString(root, "ConsumerName"),
                    CommentExcerpt = GetString(root, "comment_excerpt")
                };
            }
            catch
            {
                return new AlertDetail();
            }
        }

        private static string? GetString(JsonElement root, string property)
        {
            if (!root.TryGetProperty(property, out var el)) return null;
            return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        }

        private sealed class AlertDetail
        {
            public string? ReviewerId { get; set; }
            public string? ProviderId { get; set; }
            public string? ConsumerName { get; set; }
            public string? CommentExcerpt { get; set; }
        }

        private string GetPriorityClass(string priority) =>
            priority switch
            {
                "High" => "p-high",
                "Med" => "p-med",
                _ => "p-low"
            };

        private void OpenTicket(AdminDisputeRowDto ticket)
        {
            selectedTicket = ticket;
            resolutionDraft = "";
            showModal = true;
        }

        private void CloseModal()
        {
            showModal = false;
            selectedTicket = null;
        }

        private async Task SubmitResolveAsync()
        {
            if (selectedTicket == null || selectedTicket.Status != "Pending") return;

            isSubmitting = true;
            StateHasChanged();

            var ok = await Disputes.ResolveAsync(selectedTicket.Id, resolutionDraft, default);
            isSubmitting = false;

            if (ok)
            {
                Toast.ShowToast("Dispute", "Marked as resolved.", "success");
                CloseModal();
                await ReloadAsync();
            }
            else
            {
                Toast.ShowToast("Dispute", "Could not resolve. Check database RPC and permissions.", "error");
            }
        }
    }
}
