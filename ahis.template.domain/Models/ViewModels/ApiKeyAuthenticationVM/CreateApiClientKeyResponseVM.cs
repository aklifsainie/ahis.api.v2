using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM
{
    public class CreateApiClientKeyResponseVM
    {
        public Guid KeyId { get; init; }

        public Guid ApiClientId { get; init; }

        public string KeyName { get; init; } = string.Empty;

        public string KeyPrefix { get; init; } = string.Empty;

        /// <summary>
        /// Returned only once.
        /// </summary>
        public string ApiKey { get; init; } = string.Empty;

        public DateTime? ExpiresAt { get; init; }
    }
}
