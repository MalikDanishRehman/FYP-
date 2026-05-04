using AI_Driven_Water_Supply.Application.Common;
using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public sealed class AdminProfileModerationService : IAdminProfileModerationService
    {
        private const int MaxPageSize = 100;
        private readonly Client _supabase;
        private readonly IAdminAccessService _adminAccess;
        private readonly IAdminAuditService _audit;

        public AdminProfileModerationService(Client supabase, IAdminAccessService adminAccess, IAdminAuditService audit)
        {
            _supabase = supabase;
            _adminAccess = adminAccess;
            _audit = audit;
        }

        public async Task<AdminProfileCountsDto?> GetProfileCountsAsync(CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return null;

            try
            {
                var row = await _supabase.Rpc<CountsRpcRow>("admin_profile_counts", null).ConfigureAwait(false);
                if (row == null) return null;

                return new AdminProfileCountsDto
                {
                    Consumers = row.Consumers,
                    Providers = row.Providers,
                    Banned = row.Banned,
                    ActiveConsumers = row.ActiveConsumers
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<PagedResult<Profile>> ListConsumersAsync(
            int page,
            int pageSize,
            string? searchUsername,
            CancellationToken cancellationToken = default)
        {
            return await ListByRoleAsync("Consumer", page, pageSize, searchUsername, cancellationToken).ConfigureAwait(false);
        }

        public async Task<PagedResult<Profile>> ListProvidersAsync(
            int page,
            int pageSize,
            string? searchUsername,
            CancellationToken cancellationToken = default)
        {
            return await ListByRoleAsync("Provider", page, pageSize, searchUsername, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> SetAccountStatusAsync(string profileId, string accountStatus, CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return false;

            if (string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(accountStatus))
                return false;

            try
            {
                var args = new Dictionary<string, object>
                {
                    { "p_target", Guid.Parse(profileId) },
                    { "p_status", accountStatus.Trim().ToLowerInvariant() }
                };

                await _supabase.Rpc("admin_set_account_status", args).ConfigureAwait(false);

                await _audit.LogAsync(
                    "set_account_status",
                    "profile",
                    profileId,
                    JsonSerializer.Serialize(new { account_status = accountStatus }),
                    cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<PagedResult<Profile>> ListByRoleAsync(
            string role,
            int page,
            int pageSize,
            string? searchUsername,
            CancellationToken cancellationToken)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return new PagedResult<Profile> { Page = page, PageSize = pageSize };

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var from = (page - 1) * pageSize;
            var to = from + pageSize;

            try
            {
                var query = _supabase.From<Profile>()
                    .Where(x => x.Role == role)
                    .Order("username", Supabase.Postgrest.Constants.Ordering.Ascending);

                if (!string.IsNullOrWhiteSpace(searchUsername))
                {
                    var term = searchUsername.Trim().Replace("%", "").Replace("_", "");
                    if (term.Length > 0)
                        query = query.Filter("username", Supabase.Postgrest.Constants.Operator.ILike, $"%{term}%");
                }

                var response = await query.Range(from, to).Get().ConfigureAwait(false);
                var models = response.Models;
                var hasNext = models.Count > pageSize;
                var items = models.Take(pageSize).ToList();

                return new PagedResult<Profile>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    HasNextPage = hasNext
                };
            }
            catch
            {
                return new PagedResult<Profile> { Page = page, PageSize = pageSize };
            }
        }

        private sealed class CountsRpcRow
        {
            [JsonPropertyName("consumers")]
            public long Consumers { get; set; }

            [JsonPropertyName("providers")]
            public long Providers { get; set; }

            [JsonPropertyName("banned")]
            public long Banned { get; set; }

            [JsonPropertyName("active_consumers")]
            public long ActiveConsumers { get; set; }
        }
    }
}
