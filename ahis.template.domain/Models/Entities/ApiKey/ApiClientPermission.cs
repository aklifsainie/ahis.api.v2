using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.Entities.ApiKey
{
    public sealed class ApiClientPermission
    {
        public Guid Id { get; set; }

        public Guid ApiClientId { get; set; }

        /// <summary>
        /// Examples:
        /// complaint.write
        /// complaint.read
        /// announcement.read
        /// appointment.write
        /// </summary>
        public string PermissionCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public ApiClient ApiClient { get; set; } = null!;
    }
}
