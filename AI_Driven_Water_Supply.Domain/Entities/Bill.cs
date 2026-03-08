using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("bills")]
    public class Bill : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("order_id")]
        public long OrderId { get; set; }

        [Column("consumer_name")]
        public string ConsumerName { get; set; } = string.Empty;

        [Column("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Unpaid";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("payment_method")]
        public string? PaymentMethod { get; set; }
    }
}
