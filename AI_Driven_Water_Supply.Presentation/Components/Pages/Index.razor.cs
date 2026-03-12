using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Index
    {
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private Supabase.Client _supabase { get; set; } = default!;

        [Table("profiles")]
        public class UserProfile : BaseModel
        {
            [Column("id")] public string Id { get; set; } = "";
            [Column("role")] public string Role { get; set; } = "";
        }

        private async Task HandleGetStarted()
        {
            var user = AuthService.CurrentUser;
            if (user == null)
            {
                await AuthService.TryRefreshSession();
                user = AuthService.CurrentUser;
            }

            if (user != null)
            {
                try
                {
                    var response = await _supabase.From<UserProfile>().Where(x => x.Id == user.Id).Get();
                    var profile = response.Models.FirstOrDefault();

                    if (profile != null)
                    {
                        if (profile.Role == "Consumer") Nav.NavigateTo("/Consumer");
                        else Nav.NavigateTo("/ProviderDashboard");
                        return;
                    }
                }
                catch { }
            }

            Nav.NavigateTo("/get-started");
        }
    }
}
