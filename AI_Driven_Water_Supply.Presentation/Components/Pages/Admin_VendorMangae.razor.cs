using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Admin_VendorMangae : ComponentBase
    {
        [Inject] private IAdminProfileModerationService Profiles { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        protected List<Profile> Vendors { get; set; } = new();
        protected bool IsLoading { get; set; } = true;
        protected string searchTerm = "";

        protected IEnumerable<Profile> FilteredVendors =>
            string.IsNullOrWhiteSpace(searchTerm)
                ? Vendors
                : Vendors.Where(v =>
                    !string.IsNullOrEmpty(v.Username) &&
                    v.Username.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase));

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

            await FetchAsync();
        }

        private async Task FetchAsync()
        {
            IsLoading = true;
            StateHasChanged();

            var page = await Profiles.ListProvidersAsync(1, 200, null, default);
            Vendors = page.Items.ToList();

            IsLoading = false;
            StateHasChanged();
        }

        protected async Task ToggleBanAsync(Profile vendor)
        {
            var next = string.Equals(vendor.AccountStatus, "banned", System.StringComparison.OrdinalIgnoreCase)
                ? "active"
                : "banned";

            var ok = await Profiles.SetAccountStatusAsync(vendor.Id, next, default);
            if (ok)
            {
                Toast.ShowToast("Vendor", next == "banned" ? "Vendor banned." : "Vendor reactivated.", "success");
                await FetchAsync();
            }
            else
            {
                Toast.ShowToast("Vendor", "Update failed. Run SQL migration and check RPC.", "error");
            }
        }

        protected void ViewVendorStub(Profile vendor)
        {
            Toast.ShowToast("Vendor", $"Profile deep-link not configured for {vendor.Username}.", "info");
        }

        protected static string IdPreview(string id) =>
            string.IsNullOrEmpty(id) ? "—" : (id.Length <= 8 ? id : id.Substring(0, 8) + "...");
    }
}
