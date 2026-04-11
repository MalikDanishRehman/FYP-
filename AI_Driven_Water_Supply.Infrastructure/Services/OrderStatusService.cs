using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public class OrderStatusService : IOrderStatusService
    {
        private readonly Client _supabase;

        public OrderStatusService(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<bool> TryUpdateOrderStatusAsync(long orderId, string newStatus, string actingSupplierName)
        {
            var ordResponse = await _supabase.From<Order>().Where(x => x.Id == orderId).Get();
            var order = ordResponse.Models.FirstOrDefault();
            if (order == null) return false;
            if (!string.Equals(order.SupplierName, actingSupplierName, StringComparison.OrdinalIgnoreCase))
                return false;

            var current = order.Status ?? "";
            if (!IsValidTransition(current, newStatus))
                return false;

            await _supabase.From<Order>()
                .Where(x => x.Id == orderId)
                .Set(x => x.Status, newStatus)
                .Update();

            if (newStatus == "Accepted")
            {
                var refresh = await _supabase.From<Order>().Where(x => x.Id == orderId).Get();
                var orderData = refresh.Models.FirstOrDefault();
                if (orderData != null)
                {
                    var newBill = new Bill
                    {
                        OrderId = orderId,
                        ConsumerName = order.ConsumerName,
                        SupplierName = order.SupplierName,
                        Amount = (decimal)orderData.TotalPrice,
                        Status = "Unpaid",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _supabase.From<Bill>().Insert(newBill);
                }
            }

            var supplierName = order.SupplierName ?? "";
            var systemMsg = newStatus switch
            {
                "Accepted" => $"✅ Order Accepted by {supplierName}. Bill Generated.",
                "Out for Delivery" => $"🚚 Order is Out for Delivery!",
                "Completed" => $"🎉 Order Delivered Successfully.",
                "Cancelled" => $"❌ Order was Cancelled.",
                _ => $"Status updated to {newStatus}"
            };

            await _supabase.From<Message>().Insert(new Message
            {
                OrderId = orderId,
                SenderName = "System",
                ReceiverName = order.ConsumerName,
                Content = systemMsg,
                CreatedAt = DateTime.UtcNow
            });

            return true;
        }

        private static bool IsValidTransition(string current, string next) => (current, next) switch
        {
            ("Pending", "Accepted") or ("Pending", "Cancelled") => true,
            ("Accepted", "Out for Delivery") => true,
            ("Out for Delivery", "Completed") => true,
            _ => false
        };
    }
}
