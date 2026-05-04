using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
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
            tickets = page.Items.ToList();

            var metrics = await Dashboard.GetMetricsAsync();
            pendingFromMetrics = metrics?.PendingDisputes ?? tickets.Count(t => t.Status == "Pending");

            isLoading = false;
            StateHasChanged();
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
