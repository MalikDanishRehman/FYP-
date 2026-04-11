namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IOrderStatusService
    {
        /// <summary>
        /// Updates order status when the acting user is the supplier. On Accepted, creates a bill; posts a system message to the consumer.
        /// </summary>
        /// <returns>True if the update was applied.</returns>
        Task<bool> TryUpdateOrderStatusAsync(long orderId, string newStatus, string actingSupplierName);
    }
}
