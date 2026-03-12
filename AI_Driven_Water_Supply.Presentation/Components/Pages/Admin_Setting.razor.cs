using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Admin_Setting
    {
        private string adminName = "Bilal Admin";
        private string adminEmail = "admin@waterapp.com";
        private bool showToast = false;

        private async Task SaveChanges()
        {
            showToast = true;
            await Task.Delay(3000);
            showToast = false;
            StateHasChanged();
        }
    }
}
