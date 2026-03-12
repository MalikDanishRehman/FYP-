using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Chat
    {
        [Parameter] public long OrderId { get; set; }

        [Inject] public Supabase.Client SupabaseClient { get; set; } = null!;
        [Inject] public Supabase.Client _supabase { get; set; } = null!;
        [Inject] public NavigationManager Nav { get; set; } = null!;
        [Inject] public AI_Driven_Water_Supply.Application.Interfaces.IAuthService AuthService { get; set; } = null!;
        [Inject] public IJSRuntime JS { get; set; } = null!;

        private string MyName = "";
        private string SupplierName = "";
        private string ConsumerName = "";
        private string DisplayChatName = "Loading...";
        private string OrderStatus = "";
        private string newMessageInput = "";
        private bool isLoading = true;
        private bool AmISupplier = false;
        private List<Message> messages = new();

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null) { await AuthService.TryRefreshSession(); user = AuthService.CurrentUser; }

            if (user != null)
            {
                string fetchedName = "";
                try
                {
                    var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get();
                    var profile = response.Models.FirstOrDefault();
                    if (profile != null) fetchedName = profile.Username ?? "";
                }
                catch { }

                if (string.IsNullOrEmpty(fetchedName) && user.UserMetadata != null)
                    if (user.UserMetadata.TryGetValue("username", out var nameObj)) fetchedName = nameObj?.ToString() ?? "";

                if (string.IsNullOrEmpty(fetchedName) && !string.IsNullOrEmpty(user.Email)) fetchedName = user.Email.Split('@')[0];

                MyName = fetchedName;
            }
            else
            {
                Nav.NavigateTo("/login");
                return;
            }

            await LoadOrderDetails();
            await LoadMessages();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (messages.Count > 0)
            {
                try { await JS.InvokeVoidAsync("scrollToBottom", "chatList"); } catch { }
            }
        }

        private async Task LoadOrderDetails()
        {
            try
            {
                var response = await _supabase.From<Order>().Where(x => x.Id == OrderId).Get();
                var order = response.Models.FirstOrDefault();
                if (order != null)
                {
                    ConsumerName = order.ConsumerName;
                    SupplierName = order.SupplierName;
                    OrderStatus = order.Status;
                    AmISupplier = string.Equals(MyName, SupplierName, StringComparison.OrdinalIgnoreCase);
                    DisplayChatName = AmISupplier ? ConsumerName : SupplierName;
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
        }

        private async Task LoadMessages()
        {
            isLoading = true;
            try
            {
                var response = await _supabase.From<Message>()
                                            .Where(x => x.OrderId == OrderId)
                                            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                                            .Get();
                messages = response.Models;
            }
            finally { isLoading = false; }
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(newMessageInput)) return;

            var content = newMessageInput;
            newMessageInput = string.Empty;

            var tempMsg = new Message
            {
                SenderName = MyName,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            messages.Add(tempMsg);

            await JS.InvokeVoidAsync("scrollToBottom", "chatList");

            await PostMessage(MyName, AmISupplier ? ConsumerName : SupplierName, content);
        }

        private async Task PostMessage(string sender, string receiver, string content)
        {
            var newMsg = new Message
            {
                OrderId = OrderId,
                SenderName = sender,
                ReceiverName = receiver,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            await _supabase.From<Message>().Insert(newMsg);
        }

        private async Task UpdateStatus(string newStatus)
        {
            try
            {
                await _supabase.From<Order>()
                               .Where(x => x.Id == OrderId)
                               .Set(x => x.Status, newStatus)
                               .Update();

                OrderStatus = newStatus;

                if (newStatus == "Accepted")
                {
                    var ordResponse = await _supabase.From<Order>().Where(x => x.Id == OrderId).Get();
                    var orderData = ordResponse.Models.FirstOrDefault();
                    if (orderData != null)
                    {
                        var newBill = new Bill
                        {
                            OrderId = OrderId,
                            ConsumerName = ConsumerName,
                            SupplierName = SupplierName,
                            Amount = (decimal)orderData.TotalPrice,
                            Status = "Unpaid",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _supabase.From<Bill>().Insert(newBill);
                    }
                }

                string systemMsg = newStatus switch
                {
                    "Accepted" => $"✅ Order Accepted by {SupplierName}. Bill Generated.",
                    "Out for Delivery" => $"🚚 Order is Out for Delivery!",
                    "Completed" => $"🎉 Order Delivered Successfully.",
                    "Cancelled" => $"❌ Order was Cancelled.",
                    _ => $"Status updated to {newStatus}"
                };

                await PostMessage("System", ConsumerName, systemMsg);
                await LoadMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Status Update Error: " + ex.Message);
            }
        }

        private string GetStatusColor(string status) => status switch
        {
            "Pending" => "bg-warning",
            "Accepted" => "bg-info",
            "Out for Delivery" => "bg-primary",
            "Completed" => "bg-success",
            "Cancelled" => "bg-danger",
            _ => "bg-secondary"
        };

        void GoBack() => Nav.NavigateTo(AmISupplier ? "/ProviderDashboard" : "/Consumer");
    }
}
