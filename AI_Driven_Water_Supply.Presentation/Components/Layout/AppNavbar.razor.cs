using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Presentation.Components.Layout
{
    public partial class AppNavbar : IDisposable
    {
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private string _role = ""; // "", "Consumer", "Provider", "admin"
        private string _userName = "";
        private string _profileImageUrl = "/images/fallbackimg.jpg";
        private bool _profileLoaded;

        // Consumer: messages + profile dropdowns
        private bool _showMsgDropdown;
        private bool _showProfileDropdown;
        private bool _consumerChatLoading = true;
        private List<ConsumerChatViewModel> _consumerChatList = new();

        // Provider: sidebar + profile dropdown
        private bool _providerSidebarOpen;
        private bool _showProviderProfileDropdown;
        private bool _providerChatLoading = true;
        private List<Message> _providerChatList = new();

        private sealed class ConsumerChatViewModel
        {
            public long OrderId { get; set; }
            public string SupplierName { get; set; } = "";
            public string LastMessage { get; set; } = "";
            public string LastMsgTime { get; set; } = "";
            public bool IsLastMsgFromMe { get; set; }
            public int UnreadCount { get; set; }
        }

        protected override void OnInitialized()
        {
            AuthService.OnChange += OnAuthChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await AuthService.TryRefreshSession();
                    await LoadUserAndRole();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AppNavbar auth error: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            AuthService.OnChange -= OnAuthChanged;
        }

        private void OnAuthChanged()
        {
            _ = InvokeAsync(async () =>
            {
                await LoadUserAndRole();
                StateHasChanged();
            });
        }

        private async Task LoadUserAndRole()
        {
            var user = AuthService.CurrentUser;
            _role = "";
            _userName = "";
            _profileImageUrl = "/images/fallbackimg.jpg";
            _profileLoaded = false;
            _consumerChatList.Clear();
            _providerChatList.Clear();

            if (user == null)
            {
                StateHasChanged();
                return;
            }

            try
            {
                var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get();
                var profile = response.Models.FirstOrDefault();
                if (profile != null)
                {
                    _role = profile.Role ?? "";
                    _userName = profile.Username ?? "";
                    if (!string.IsNullOrEmpty(profile.ProfilePic))
                        _profileImageUrl = _supabase.Storage.From("Avatar").GetPublicUrl(profile.ProfilePic);
                }

                if (string.IsNullOrEmpty(_userName) && user.UserMetadata != null && user.UserMetadata.TryGetValue("username", out var nameObj))
                    _userName = nameObj?.ToString() ?? "";
                if (string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(user.Email))
                    _userName = user.Email.Split('@')[0];
                if (string.IsNullOrEmpty(_userName))
                    _userName = _role == "Provider" ? "Provider" : "User";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AppNavbar profile error: {ex.Message}");
            }

            _profileLoaded = true;

            if (_role == "Consumer")
                await LoadConsumerChats();
            else if (_role == "Provider")
                await LoadProviderMessages();

            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadConsumerChats()
        {
            if (string.IsNullOrEmpty(_userName)) return;
            _consumerChatLoading = true;
            StateHasChanged();
            string nameWithoutSpace = _userName.Replace(" ", "");
            _consumerChatList.Clear();
            try
            {
                var orderResponse = await _supabase.From<Order>()
                    .Where(x => x.ConsumerName == nameWithoutSpace)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                foreach (var order in orderResponse.Models)
                {
                    var msgResponse = await _supabase.From<Message>()
                        .Where(x => x.OrderId == order.Id)
                        .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                        .Limit(1)
                        .Get();
                    var lastMsg = msgResponse.Models.FirstOrDefault();
                    _consumerChatList.Add(new ConsumerChatViewModel
                    {
                        OrderId = order.Id,
                        SupplierName = order.SupplierName ?? "",
                        LastMessage = lastMsg != null ? lastMsg.Content ?? "" : "Order placed",
                        LastMsgTime = lastMsg != null ? lastMsg.CreatedAt.ToLocalTime().ToString("hh:mm tt") : order.CreatedAt.ToLocalTime().ToString("hh:mm tt"),
                        IsLastMsgFromMe = lastMsg != null && lastMsg.SenderName == _userName
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine("Consumer chat load: " + ex.Message); }
            finally { _consumerChatLoading = false; }
        }

        private async Task LoadProviderMessages()
        {
            if (_userName == "Loading..." || _userName == "Provider") return;
            _providerChatLoading = true;
            StateHasChanged();
            try
            {
                var response = await _supabase.From<Message>()
                    .Where(x => x.ReceiverName == _userName)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                _providerChatList = response.Models.GroupBy(m => m.OrderId).Select(g => g.First()).ToList();
            }
            catch { }
            finally { _providerChatLoading = false; }
        }

        private void ToggleMsgDropdown()
        {
            _showMsgDropdown = !_showMsgDropdown;
            _showProfileDropdown = false;
        }

        private void ToggleProfileDropdown()
        {
            _showProfileDropdown = !_showProfileDropdown;
            _showMsgDropdown = false;
        }

        private void OpenChat(long orderId) => Nav.NavigateTo($"/chat/{orderId}");

        private async Task RefreshConsumerChats() => await LoadConsumerChats();

        private void ToggleProviderSidebar() => _providerSidebarOpen = !_providerSidebarOpen;

        private void ToggleProviderProfileDropdown() => _showProviderProfileDropdown = !_showProviderProfileDropdown;

        private async Task HandleLogout()
        {
            await AuthService.SignOut();
            Nav.NavigateTo("/login", forceLoad: true);
        }

        private bool IsGuest => !_profileLoaded || string.IsNullOrEmpty(_role) || _role == "admin";
        private bool IsConsumer => _role == "Consumer";
        private bool IsProvider => _role == "Provider";
    }
}
