using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Presentation.Components.Layout
{
    public partial class AppNavbar : IDisposable
    {
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IOrderStatusService OrderStatusService { get; set; } = default!;
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
        private List<ProviderInboxRow> _providerChatList = new();

        private sealed class ConsumerChatViewModel
        {
            public long OrderId { get; set; }
            public string SupplierName { get; set; } = "";
            public string? SupplierAvatarUrl { get; set; }
            public string LastMessage { get; set; } = "";
            public string LastMsgTime { get; set; } = "";
            public bool IsLastMsgFromMe { get; set; }
            public int UnreadCount { get; set; }
        }

        private sealed class ProviderInboxRow
        {
            public long OrderId { get; set; }
            public string PreviewContent { get; set; } = "";
            public DateTime PreviewCreatedAt { get; set; }
            public string ConsumerDisplayName { get; set; } = "";
            public string Status { get; set; } = "";
            public string? PeerAvatarUrl { get; set; }
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

        private static string NormalizeLookupKey(string s) =>
            s.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();

        private async Task<string?> ResolveAvatarUrlAsync(string? primary, string? secondary, Dictionary<string, string> cache)
        {
            var candidates = new List<string>();
            void Add(string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var t = value.Trim();
                if (!candidates.Exists(c => string.Equals(c, t, StringComparison.OrdinalIgnoreCase)))
                    candidates.Add(t);
                var noSpace = t.Replace(" ", "", StringComparison.Ordinal);
                if (!string.Equals(noSpace, t, StringComparison.Ordinal) &&
                    !candidates.Exists(c => string.Equals(c, noSpace, StringComparison.OrdinalIgnoreCase)))
                    candidates.Add(noSpace);
            }

            Add(primary);
            Add(secondary);

            foreach (var candidate in candidates)
            {
                var lookupKey = NormalizeLookupKey(candidate);
                if (cache.TryGetValue(lookupKey, out var cached))
                    return string.IsNullOrEmpty(cached) ? null : cached;

                string? url = null;
                try
                {
                    var profileResponse = await _supabase.From<Profile>().Where(x => x.Username == candidate).Get();
                    var p = profileResponse.Models.FirstOrDefault();
                    if (!string.IsNullOrEmpty(p?.ProfilePic))
                        url = _supabase.Storage.From("Avatar").GetPublicUrl(p.ProfilePic);
                }
                catch { }

                cache[lookupKey] = url ?? "";
                if (url != null) return url;
            }

            return null;
        }

        private async Task LoadConsumerChats()
        {
            if (string.IsNullOrEmpty(_userName)) return;
            _consumerChatLoading = true;
            StateHasChanged();
            string nameWithoutSpace = _userName.Replace(" ", "");
            _consumerChatList.Clear();
            var avatarCache = new Dictionary<string, string>();
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
                    var supplierAvatar = await ResolveAvatarUrlAsync(order.SupplierName, order.SupplierName, avatarCache);
                    _consumerChatList.Add(new ConsumerChatViewModel
                    {
                        OrderId = order.Id,
                        SupplierName = order.SupplierName ?? "",
                        SupplierAvatarUrl = supplierAvatar,
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
                var grouped = response.Models.GroupBy(m => m.OrderId).Select(g => g.First()).ToList();
                var avatarCache = new Dictionary<string, string>();
                var list = new List<ProviderInboxRow>();
                foreach (var msg in grouped)
                {
                    Order? order = null;
                    try
                    {
                        var ordRes = await _supabase.From<Order>().Where(x => x.Id == msg.OrderId).Get();
                        order = ordRes.Models.FirstOrDefault();
                    }
                    catch { }

                    var displayName = !string.IsNullOrEmpty(order?.ConsumerName)
                        ? order!.ConsumerName
                        : msg.SenderName;
                    var status = order?.Status ?? "Pending";
                    var avatar = await ResolveAvatarUrlAsync(order?.ConsumerName, msg.SenderName, avatarCache);

                    list.Add(new ProviderInboxRow
                    {
                        OrderId = msg.OrderId,
                        PreviewContent = msg.Content ?? "",
                        PreviewCreatedAt = msg.CreatedAt,
                        ConsumerDisplayName = displayName,
                        Status = status,
                        PeerAvatarUrl = avatar
                    });
                }

                _providerChatList = list;
            }
            catch { }
            finally { _providerChatLoading = false; }
        }

        private async Task OnProviderInboxStatusChange(long orderId, ChangeEventArgs e)
        {
            var v = e.Value?.ToString();
            if (string.IsNullOrEmpty(v)) return;
            var ok = await OrderStatusService.TryUpdateOrderStatusAsync(orderId, v, _userName);
            if (ok)
            {
                var row = _providerChatList.FirstOrDefault(r => r.OrderId == orderId);
                if (row != null) row.Status = v;
                await InvokeAsync(StateHasChanged);
            }
        }

        private static bool InboxStatusIsReadOnly(string status) =>
            status == "Completed" || status == "Cancelled";

        /// <summary>CSS modifier for inbox status pill (e.g. app-navbar-status-pill--pending).</summary>
        private static string InboxStatusPillModifier(string status) => status switch
        {
            "Pending" => "pending",
            "Accepted" => "accepted",
            "Out for Delivery" => "out",
            "Completed" => "completed",
            "Cancelled" => "cancelled",
            _ => "default"
        };

        private static (string Value, string Label)[] InboxStatusNextActions(string status) => status switch
        {
            "Pending" => new[] { ("Accepted", "Accept"), ("Cancelled", "Cancel") },
            "Accepted" => new[] { ("Out for Delivery", "Dispatch") },
            "Out for Delivery" => new[] { ("Completed", "Finish") },
            _ => Array.Empty<(string, string)>()
        };

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
