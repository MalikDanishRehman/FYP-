using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public sealed class AdminAuditService : IAdminAuditService
    {
        private readonly Client _supabase;
        private readonly IAdminAccessService _adminAccess;

        public AdminAuditService(Client supabase, IAdminAccessService adminAccess)
        {
            _supabase = supabase;
            _adminAccess = adminAccess;
        }

        public async Task LogAsync(string action, string? entityType, string? entityId, string? payloadJson, CancellationToken cancellationToken = default)
        {
            var user = _supabase.Auth.CurrentUser;
            if (user == null) return;
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false)) return;

            try
            {
                var row = new AdminAuditLog
                {
                    AdminId = user.Id,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Payload = string.IsNullOrEmpty(payloadJson) ? null : payloadJson
                };

                await _supabase.From<AdminAuditLog>().Insert(row, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Best-effort audit; never block admin flows.
            }
        }
    }
}
