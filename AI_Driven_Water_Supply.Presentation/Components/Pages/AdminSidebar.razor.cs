using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class AdminSidebar : LayoutComponentBase
    {
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IAdminAccessService AdminAccess { get; set; } = default!;

        protected bool isSidebarOpen = true;

        protected override async Task OnInitializedAsync()
        {
            await AuthService.TryRefreshSession();
            if (AuthService.CurrentUser == null)
            {
                Nav.NavigateTo("/login", forceLoad: true);
                return;
            }

            if (!await AdminAccess.IsCurrentUserAdminAsync())
            {
                Nav.NavigateTo("/", forceLoad: true);
            }
        }

        protected void ToggleSidebar()
        {
            isSidebarOpen = !isSidebarOpen;
        }

        protected async Task HandleLogout()
        {
            await AuthService.SignOut();
            Nav.NavigateTo("/login", forceLoad: true);
        }
    }
}