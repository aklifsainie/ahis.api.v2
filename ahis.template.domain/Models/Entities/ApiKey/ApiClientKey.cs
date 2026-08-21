using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.Entities.ApiKey
{
    public sealed class ApiClientKey
    {
        public Guid Id { get; set; }

        public Guid ApiClientId { get; set; }

        /// <summary>
        /// Descriptive name for administration.
        /// Example: Production Key 2026.
        /// </summary>
        public string KeyName { get; set; } = string.Empty;

        /// <summary>
        /// Non-secret portion used to identify the key.
        /// Example: cpj_live_ab12cd34.
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// SHA-256 hexadecimal hash of the complete API key.
        /// The raw key is never stored.
        /// </summary>
        public string KeyHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime? LastUsedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? RevokedBy { get; set; }

        public string? RevocationReason { get; set; }

        public ApiClient ApiClient { get; set; } = null!;
    }
}
