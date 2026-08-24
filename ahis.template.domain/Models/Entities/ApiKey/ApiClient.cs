using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.Entities.ApiKey
{
    public sealed class ApiClient
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Stable machine-readable identifier.
        /// Example: cerita-pj-public-portal
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable external system name.
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ContactName { get; set; }

        public string? ContactEmail { get; set; }

        public bool IsActive { get; set; } = true;

        public int RateLimitPerMinute { get; set; } = 60;

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? DeactivatedAt { get; set; }

        public string? DeactivationReason { get; set; }

        public ICollection<ApiClientKey> Keys { get; set; }
            = new List<ApiClientKey>();

        public ICollection<ApiClientPermission> Permissions { get; set; }
            = new List<ApiClientPermission>();
    }
}
