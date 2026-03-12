using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class AdminPage
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private string UserName = "Loading...";
        private string ProfileImageUrl = "/images/fallbackimg.jpg";
        private bool isSidebarOpen = false;
        private bool isLoading = true;
        private int totalRevenue = 0;
        private List<Message> chatList = new List<Message>();
        private string activeTab = "Week";

        [Table("profiles")]
        public class Profile : BaseModel
        {
            [Column("id")] public string Id { get; set; } = "";
            [Column("username")] public string Username { get; set; } = "";
            [Column("profilepic")] public string ProfilePic { get; set; } = "";
        }

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null) { await AuthService.TryRefreshSession(); user = AuthService.CurrentUser; }

            if (user != null)
            {
                await SetDynamicData(user);
                StateHasChanged();
                await LoadMessages();
                isLoading = false;
                StateHasChanged();
            }
            else
            {
                Nav.NavigateTo("/login");
            }
        }

        private async Task SetDynamicData(Supabase.Gotrue.User user)
        {
            string fetchedName = "";
            try
            {
                var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get();
                var profile = response.Models.FirstOrDefault();
                if (profile != null)
                {
                    fetchedName = profile.Username;
                    if (!string.IsNullOrEmpty(profile.ProfilePic))
                    {
                        ProfileImageUrl = _supabase.Storage.From("Avatar").GetPublicUrl(profile.ProfilePic);
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(fetchedName) && user.UserMetadata != null)
                if (user.UserMetadata.TryGetValue("username", out var nameObj)) fetchedName = nameObj?.ToString();

            if (string.IsNullOrEmpty(fetchedName) && !string.IsNullOrEmpty(user.Email))
                fetchedName = user.Email.Split('@')[0];

            UserName = string.IsNullOrEmpty(fetchedName) ? "Provider" : fetchedName;
        }

        private async Task LoadMessages()
        {
            try
            {
                if (UserName == "Loading..." || UserName == "Provider") return;
                var response = await _supabase.From<Message>().Where(x => x.ReceiverName == UserName).Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending).Get();
                chatList = response.Models.GroupBy(m => m.OrderId).Select(g => g.First()).ToList();
                totalRevenue = chatList.Count * 1200;
            }
            catch { }
        }
    }
}
