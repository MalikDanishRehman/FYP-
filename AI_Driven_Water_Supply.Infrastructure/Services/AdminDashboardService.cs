using AI_Driven_Water_Supply.Application.Common;
using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public sealed class AdminDashboardService : IAdminDashboardService
    {
        private const int MaxPageSize = 100;
        private readonly Client _supabase;
        private readonly IAdminAccessService _adminAccess;

        public AdminDashboardService(Client supabase, IAdminAccessService adminAccess)
        {
            _supabase = supabase;
            _adminAccess = adminAccess;
        }

        public async Task<AdminDashboardMetricsDto?> GetMetricsAsync(CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return null;

            try
            {
                var row = await _supabase.Rpc<MetricsRpcRow>("admin_dashboard_metrics", null)
                    .ConfigureAwait(false);
                if (row == null) return null;

                return new AdminDashboardMetricsDto
                {
                    TotalRevenuePkr = row.TotalRevenuePkr,
                    ActiveVendors = row.ActiveVendors,
                    SuccessRatePercent = row.SuccessRatePercent,
                    PendingDisputes = row.PendingDisputes,
                    PendingOrders = row.PendingOrders
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<PagedResult<AdminOrderRowDto>> GetRecentOrdersAsync(
            int page,
            int pageSize,
            string? statusFilter,
            string? searchOrderId,
            CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return new PagedResult<AdminOrderRowDto> { Page = page, PageSize = pageSize };

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var from = (page - 1) * pageSize;
            var to = from + pageSize;

            try
            {
                var query = _supabase.From<Order>().Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending);

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    var s = statusFilter.Trim();
                    query = query.Where(x => x.Status == s);
                }

                if (!string.IsNullOrWhiteSpace(searchOrderId)
                    && long.TryParse(
                        searchOrderId.Trim().Replace("#", "", StringComparison.OrdinalIgnoreCase)
                            .Replace("ORD-", "", StringComparison.OrdinalIgnoreCase)
                            .Replace("ord-", "", StringComparison.OrdinalIgnoreCase),
                        out var oid))
                {
                    query = query.Where(x => x.Id == oid);
                }

                var response = await query.Range(from, to).Get().ConfigureAwait(false);
                var models = response.Models;

                var hasNext = models.Count > pageSize;
                var items = models.Take(pageSize).Select(o => new AdminOrderRowDto
                {
                    Id = o.Id,
                    ConsumerName = o.ConsumerName,
                    SupplierName = o.SupplierName,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status
                }).ToList();

                return new PagedResult<AdminOrderRowDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    HasNextPage = hasNext
                };
            }
            catch
            {
                return new PagedResult<AdminOrderRowDto> { Page = page, PageSize = pageSize };
            }
        }

        private sealed class MetricsRpcRow
        {
            [JsonPropertyName("total_revenue_pkr")]
            public long TotalRevenuePkr { get; set; }

            [JsonPropertyName("active_vendors")]
            public long ActiveVendors { get; set; }

            [JsonPropertyName("success_rate_percent")]
            public decimal SuccessRatePercent { get; set; }

            [JsonPropertyName("pending_disputes")]
            public long PendingDisputes { get; set; }

            [JsonPropertyName("pending_orders")]
            public long PendingOrders { get; set; }
        }
    }
}
