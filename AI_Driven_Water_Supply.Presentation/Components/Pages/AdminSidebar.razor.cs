using Microsoft.AspNetCore.Components;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class AdminSidebar : LayoutComponentBase
    {
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private bool isSidebarOpen = true;

        private void ToggleSidebar()
        {
            isSidebarOpen = !isSidebarOpen;
        }
    }
}
