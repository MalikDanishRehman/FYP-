using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class AdminSidebar : LayoutComponentBase
    {
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;

        protected bool isSidebarOpen = true;

        protected void ToggleSidebar()
        {
            isSidebarOpen = !isSidebarOpen;
        }

        protected async Task HandleLogout()
        {
            await AuthService.SignOut();
            // ForceLoad true rakha hai taake logout ke baad page poora refresh ho aur user login par jaye
            Nav.NavigateTo("/login", forceLoad: true);
        }
    }
}