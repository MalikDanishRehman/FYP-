using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Admin_UserManage : ComponentBase
    {
        [Inject] private IAdminProfileModerationService Profiles { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        protected List<Profile> Users { get; set; } = new();
        protected bool IsLoading { get; set; } = true;
        protected string searchTerm = "";
        private AdminProfileCountsDto? counts;

        protected IEnumerable<Profile> FilteredUsers =>
            string.IsNullOrWhiteSpace(searchTerm)
                ? Users
                : Users.Where(u =>
                    !string.IsNullOrEmpty(u.Username) &&
                    u.Username.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase));

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

            counts = await Profiles.GetProfileCountsAsync();
            var page = await Profiles.ListConsumersAsync(1, 200, null, default);
            Users = page.Items.ToList();

            IsLoading = false;
            StateHasChanged();
        }

        protected async Task ToggleBanAsync(Profile user)
        {
            var next = string.Equals(user.AccountStatus, "banned", System.StringComparison.OrdinalIgnoreCase)
                ? "active"
                : "banned";

            var ok = await Profiles.SetAccountStatusAsync(user.Id, next, default);
            if (ok)
            {
                Toast.ShowToast("User", next == "banned" ? "User banned." : "User reactivated.", "success");
                await FetchAsync();
            }
            else
            {
                Toast.ShowToast("User", "Update failed. Run SQL migration and check RPC.", "error");
            }
        }

        protected void AddUserStub()
        {
            Toast.ShowToast("Users", "Invite flow is not implemented yet.", "info");
        }

        protected void EditUserStub(Profile user)
        {
            Toast.ShowToast("Users", $"Edit profile UI is not wired for {user.Username}.", "info");
        }
    }
}
