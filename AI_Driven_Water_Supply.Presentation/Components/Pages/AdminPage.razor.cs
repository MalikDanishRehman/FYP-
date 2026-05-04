using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Supabase;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class AdminPage
    {
        [Inject] private Client SupabaseClient { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IAdminDashboardService Dashboard { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private string UserName = "Loading...";
        private string ProfileImageUrl = "/images/fallbackimg.jpg";
        private AdminDashboardMetricsDto? metrics;
        private List<AdminOrderRowDto> recentOrders = new();
        private string statusFilter = "";
        private string searchOrderId = "";
        private bool isLoadingMetrics = true;
        private bool isLoadingOrders = true;

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

            await SetDynamicData(user);
            await LoadDashboardAsync();
        }

        private async Task SetDynamicData(Supabase.Gotrue.User user)
        {
            string fetchedName = "";
            try
            {
                var response = await SupabaseClient.From<Profile>().Where(x => x.Id == user.Id).Get();
                var profile = response.Models.FirstOrDefault();
                if (profile != null)
                {
                    fetchedName = profile.Username;
                    if (!string.IsNullOrEmpty(profile.ProfilePic))
                    {
                        ProfileImageUrl = SupabaseClient.Storage.From("Avatar").GetPublicUrl(profile.ProfilePic) ?? ProfileImageUrl;
                    }
                }
            }
            catch { /* ignore */ }

            if (string.IsNullOrEmpty(fetchedName) && user.UserMetadata != null)
                if (user.UserMetadata.TryGetValue("username", out var nameObj))
                    fetchedName = nameObj?.ToString() ?? "";

            if (string.IsNullOrEmpty(fetchedName) && !string.IsNullOrEmpty(user.Email))
                fetchedName = user.Email.Split('@')[0];

            UserName = string.IsNullOrEmpty(fetchedName) ? "Admin" : fetchedName;
        }

        private async Task LoadDashboardAsync()
        {
            isLoadingMetrics = true;
            isLoadingOrders = true;
            StateHasChanged();

            metrics = await Dashboard.GetMetricsAsync();
            isLoadingMetrics = false;
            StateHasChanged();

            var ordersPage = await Dashboard.GetRecentOrdersAsync(
                1,
                25,
                string.IsNullOrWhiteSpace(statusFilter) ? null : statusFilter.Trim(),
                string.IsNullOrWhiteSpace(searchOrderId) ? null : searchOrderId.Trim(),
                default);

            recentOrders = ordersPage.Items.ToList();
            isLoadingOrders = false;
            StateHasChanged();
        }

        private async Task RefreshAsync()
        {
            await LoadDashboardAsync();
            Toast.ShowToast("Dashboard", "Refreshed.", "success");
        }

        private async Task ClearFiltersAsync()
        {
            statusFilter = "";
            searchOrderId = "";
            await LoadDashboardAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            await LoadDashboardAsync();
        }

        private void StubNotConfigured(string title)
        {
            Toast.ShowToast(title, "This action is not configured yet.", "info");
        }
    }
}
