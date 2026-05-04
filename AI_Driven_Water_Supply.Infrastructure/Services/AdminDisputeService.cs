using AI_Driven_Water_Supply.Application.Common;
using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public sealed class AdminDisputeService : IAdminDisputeService
    {
        private const int MaxPageSize = 100;
        private readonly Client _supabase;
        private readonly IAdminAccessService _adminAccess;
        private readonly IAdminAuditService _audit;

        public AdminDisputeService(Client supabase, IAdminAccessService adminAccess, IAdminAuditService audit)
        {
            _supabase = supabase;
            _adminAccess = adminAccess;
            _audit = audit;
        }

        public async Task<PagedResult<AdminDisputeRowDto>> ListAsync(
            string? statusTab,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return new PagedResult<AdminDisputeRowDto> { Page = page, PageSize = pageSize };

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var from = (page - 1) * pageSize;
            var to = from + pageSize;

            try
            {
                var query = _supabase.From<Dispute>().Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending);

                if (string.Equals(statusTab, "Pending", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(x => x.Status == "Pending");
                else if (string.Equals(statusTab, "Resolved", StringComparison.OrdinalIgnoreCase))
                    query = query.Filter("status", Supabase.Postgrest.Constants.Operator.In, "(Resolved,Closed)");

                var response = await query.Range(from, to).Get().ConfigureAwait(false);
                var models = response.Models;
                var hasNext = models.Count > pageSize;
                var slice = models.Take(pageSize).ToList();

                var names = await LoadUsernamesAsync(slice, cancellationToken).ConfigureAwait(false);

                var items = slice.Select(d => new AdminDisputeRowDto
                {
                    Id = d.Id,
                    ConsumerName = d.ConsumerId != null && names.TryGetValue(d.ConsumerId, out var cn) ? cn : null,
                    ProviderName = d.ProviderId != null && names.TryGetValue(d.ProviderId, out var pn) ? pn : null,
                    IssueType = d.IssueType,
                    Description = d.Description,
                    Priority = d.Priority,
                    Status = d.Status,
                    CreatedAt = d.CreatedAt
                }).ToList();

                return new PagedResult<AdminDisputeRowDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    HasNextPage = hasNext
                };
            }
            catch
            {
                return new PagedResult<AdminDisputeRowDto> { Page = page, PageSize = pageSize };
            }
        }

        public async Task<bool> ResolveAsync(Guid disputeId, string resolutionNotes, CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return false;

            try
            {
                var args = new Dictionary<string, object>
                {
                    { "p_id", disputeId },
                    { "p_notes", resolutionNotes ?? "" }
                };

                await _supabase.Rpc("admin_resolve_dispute", args).ConfigureAwait(false);

                await _audit.LogAsync(
                    "resolve_dispute",
                    "dispute",
                    disputeId.ToString(),
                    JsonSerializer.Serialize(new { notes = resolutionNotes }),
                    cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<Dictionary<string, string>> LoadUsernamesAsync(IReadOnlyList<Dispute> disputes, CancellationToken cancellationToken)
        {
            var ids = new HashSet<string>();
            foreach (var d in disputes)
            {
                if (!string.IsNullOrEmpty(d.ConsumerId)) ids.Add(d.ConsumerId);
                if (!string.IsNullOrEmpty(d.ProviderId)) ids.Add(d.ProviderId);
            }

            if (ids.Count == 0) return new Dictionary<string, string>();

            var list = ids.ToList();
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var chunk in list.Chunk(30))
            {
                var inList = "(" + string.Join(",", chunk) + ")";
                var resp = await _supabase.From<Profile>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.In, inList)
                    .Get()
                    .ConfigureAwait(false);
                foreach (var p in resp.Models)
                    map[p.Id] = p.Username ?? "";
            }

            return map;
        }
    }
}
