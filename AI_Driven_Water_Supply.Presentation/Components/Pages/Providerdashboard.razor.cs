using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Presentation;
using Microsoft.JSInterop;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages // ⚠️ Check Namespace
{
    public partial class ProviderDashboard : ComponentBase
    {
        [Inject] public IAuthService AuthService { get; set; } = default!;
        [Inject] public Supabase.Client _supabase { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        protected string UserName = "Loading...";
        protected bool isSidebarOpen = false;
        protected bool isLoading = true;
        protected int totalRevenue = 0;
        protected List<Message> chatList = new List<Message>();

        // Local Profile Model (Agar Application layer me hai to wahan se import karein)
        [Table("profiles")]
        public class Profile : BaseModel
        {
            [Column("id")] public string Id { get; set; }
            [Column("username")] public string Username { get; set; }
        }

        protected override async Task OnInitializedAsync()
        {
            // Session Check
            var user = AuthService.CurrentUser;
            if (user == null)
            {
                await AuthService.TryRefreshSession();
                user = AuthService.CurrentUser;
            }

            if (user != null)
            {
                await SetDynamicUserName(user);
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

        private async Task SetDynamicUserName(Supabase.Gotrue.User user)
        {
            string fetchedName = "";
            try
            {
                var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get();
                var profile = response.Models.FirstOrDefault();
                if (profile != null) fetchedName = profile.Username;
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

                var response = await _supabase
                    .From<Message>()
                    .Where(x => x.ReceiverName == UserName)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                var allMessages = response.Models;

                chatList = allMessages
                            .GroupBy(m => m.OrderId)
                            .Select(g => g.First())
                            .ToList();

                totalRevenue = chatList.Count * 1200;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        protected void ToggleSidebar() => isSidebarOpen = !isSidebarOpen;
        protected void OpenChat(long orderId) => Nav.NavigateTo($"/chat/{orderId}");

        protected async Task HandleLogout()
        {
            await AuthService.SignOut();
            Nav.NavigateTo("/login");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("eval", @"
                    var ctx = document.getElementById('revenueChart');
                    if(ctx) {
                        new Chart(ctx.getContext('2d'), {
                            type: 'line',
                            data: {
                                labels: ['M','T','W','T','F','S','S'],
                                datasets: [{
                                    label: 'Revenue', data: [12, 19, 3, 5, 2, 3, 10],
                                    borderColor: '#BFFF27', tension: 0.4, fill: true,
                                    backgroundColor: 'rgba(191, 255, 39, 0.1)'
                                }]
                            },
                            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { x: { display: false }, y: { display: false } } }
                        });
                    }
                ");
            }
        }
    }
}