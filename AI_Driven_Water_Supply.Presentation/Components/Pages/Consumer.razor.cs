using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Consumer
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;

        private string MyName = "";

        protected override async Task OnInitializedAsync()
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
                    var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get();
                    var profile = response.Models.FirstOrDefault();

                    if (profile != null)
                        MyName = profile.Username ?? "";

                    if (string.IsNullOrEmpty(MyName) && user.UserMetadata != null && user.UserMetadata.ContainsKey("username"))
                        MyName = user.UserMetadata["username"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(MyName) && !string.IsNullOrEmpty(user.Email))
                        MyName = user.Email.Split('@')[0];
                }
                catch (Exception ex) { Console.WriteLine("Profile Error: " + ex.Message); }
            }
        }

        private void GoToBotPage() => Nav.NavigateTo("/Helper_Agent");
    }
}
