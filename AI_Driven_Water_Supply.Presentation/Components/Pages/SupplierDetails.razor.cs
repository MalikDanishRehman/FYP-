using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Presentation; // For Order/Message models
using Supabase.Postgrest.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages // ⚠️ CHANGE THIS to your actual namespace
{
    public partial class Supplier : ComponentBase
    {
        // Dependency Injection in Code-Behind uses [Inject]
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public Supabase.Client _supabase { get; set; } = default!;
        [Inject] public IAuthService AuthService { get; set; } = default!;

        [Parameter] public string name { get; set; } = default!;

        // Variables
        protected string description = "";
        protected int quantity = 1;
        protected int pricePerBottle = 120;
        protected int totalPrice = 120;
        protected bool showModal = false;

        // Form Model
        protected OrderRequestForm orderFormModel = new OrderRequestForm();

        public class OrderRequestForm
        {
            public string ConsumerName { get; set; } = "";
            public string Address { get; set; } = "";
            public string Phone { get; set; } = "";
        }

        protected override void OnInitialized()
        {
            if (string.IsNullOrEmpty(name)) name = "Unknown Supplier";
            description = $"Reliable service by {name}. Delivery usually within 2 hours.";
            Calculate();

            // 🔥 ROBUST NAME FETCHING
            var user = AuthService.CurrentUser;
            if (user != null)
            {
                string fetchedName = "";

                // 1. Try Metadata
                if (user.UserMetadata != null && user.UserMetadata.TryGetValue("username", out var nameObj))
                {
                    fetchedName = nameObj?.ToString() ?? "";
                }

                // 2. Fallback to Email Prefix
                if (string.IsNullOrEmpty(fetchedName) && !string.IsNullOrEmpty(user.Email))
                {
                    fetchedName = user.Email.Split('@')[0];
                }

                orderFormModel.ConsumerName = fetchedName;
            }
        }

        protected void UpdateQty(int change)
        {
            if (quantity + change >= 1) { quantity += change; Calculate(); }
        }

        protected void Calculate() => totalPrice = quantity * pricePerBottle;
        protected void OpenOrderModal() => showModal = true;
        protected void CloseModal() => showModal = false;

        protected async Task HandleOrderSubmit()
        {
            if (string.IsNullOrWhiteSpace(orderFormModel.Address) || string.IsNullOrWhiteSpace(orderFormModel.Phone)) return;

            try
            {
                // 1. Create Order
                var newOrder = new AI_Driven_Water_Supply.Presentation.Order
                {
                    ConsumerName = orderFormModel.ConsumerName,
                    Address = orderFormModel.Address,
                    Phone = orderFormModel.Phone,
                    Quantity = quantity,
                    ItemName = "Water Bottle",
                    TotalPrice = totalPrice,
                    SupplierName = name,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                var response = await _supabase.From<AI_Driven_Water_Supply.Presentation.Order>().Insert(newOrder);
                var insertedOrder = response.Models.FirstOrDefault();

                // 2. If Order Created, Create First Message
                if (insertedOrder != null)
                {
                    var autoMsg = new AI_Driven_Water_Supply.Presentation.Message
                    {
                        OrderId = insertedOrder.Id,
                        SenderName = orderFormModel.ConsumerName,
                        ReceiverName = name,
                        Content = $"New Order! {quantity}x Bottles. Address: {orderFormModel.Address}",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _supabase.From<AI_Driven_Water_Supply.Presentation.Message>().Insert(autoMsg);

                    showModal = false;
                    Nav.NavigateTo("/Consumer");
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        }
    }
}