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
        private string ProfileImageUrl = "/images/fallbackimg.jpg";
        private bool showMsgDropdown = false;
        private bool showProfileDropdown = false;
        private bool isLoading = true;
#pragma warning disable CS0414
        private bool isBotOpen = false;
#pragma warning restore CS0414

        private List<ChatViewModel> chatList = new();

        private class ChatViewModel
        {
            public long OrderId { get; set; }
            public string SupplierName { get; set; } = "";
            public string LastMessage { get; set; } = "";
            public string LastMsgTime { get; set; } = "";
            public bool IsLastMsgFromMe { get; set; }
            public int UnreadCount { get; set; }
        }

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
                    {
                        MyName = profile.Username ?? "";
                        if (!string.IsNullOrEmpty(profile.ProfilePic))
                        {
                            ProfileImageUrl = _supabase.Storage.From("Avatar").GetPublicUrl(profile.ProfilePic);
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine("Profile Error: " + ex.Message); }

                if (string.IsNullOrEmpty(MyName) && user.UserMetadata != null && user.UserMetadata.ContainsKey("username"))
                    MyName = user.UserMetadata["username"]?.ToString() ?? "";

                if (string.IsNullOrEmpty(MyName) && !string.IsNullOrEmpty(user.Email))
                    MyName = user.Email.Split('@')[0];

                await LoadChats();
            }
        }

        private async Task LoadChats()
        {
            if (string.IsNullOrEmpty(MyName)) return;
            isLoading = true;
            string nameWithoutSpace = MyName.Replace(" ", "");

            chatList.Clear();
            try
            {
                var orderResponse = await _supabase.From<Order>()
                    .Where(x => x.ConsumerName == nameWithoutSpace)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                var myOrders = orderResponse.Models;

                foreach (var order in myOrders)
                {
                    var msgResponse = await _supabase.From<Message>()
                        .Where(x => x.OrderId == order.Id)
                        .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                        .Limit(1)
                        .Get();

                    var lastMsg = msgResponse.Models.FirstOrDefault();
                    chatList.Add(new ChatViewModel
                    {
                        OrderId = order.Id,
                        SupplierName = order.SupplierName ?? "",
                        LastMessage = lastMsg != null ? lastMsg.Content ?? "" : "Order placed",
                        LastMsgTime = lastMsg != null ? lastMsg.CreatedAt.ToLocalTime().ToString("hh:mm tt") : order.CreatedAt.ToLocalTime().ToString("hh:mm tt"),
                        IsLastMsgFromMe = lastMsg != null && lastMsg.SenderName == MyName
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine("Chat Load Error: " + ex.Message); }
            finally { isLoading = false; }
        }

        private void ToggleMsgDropdown() { showMsgDropdown = !showMsgDropdown; showProfileDropdown = false; }
        private void ToggleProfileDropdown() { showProfileDropdown = !showProfileDropdown; showMsgDropdown = false; }
        private void OpenChat(long orderId) { Nav.NavigateTo($"/chat/{orderId}"); }
        private async Task RefreshChats() { await LoadChats(); }
        private async Task HandleLogout() { await AuthService.SignOut(); Nav.NavigateTo("/login"); }
        private void GoToBotPage() => Nav.NavigateTo("/Helper_Agent");
    }
}
