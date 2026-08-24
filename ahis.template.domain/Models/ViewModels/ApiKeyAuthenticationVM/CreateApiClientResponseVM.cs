using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM
{
    public class CreateApiClientResponseVM
    {
        public Guid Id { get; init; }

        public string ClientId { get; init; } = string.Empty;

        public string ClientName { get; init; } = string.Empty;

        public string KeyName { get; init; } = string.Empty;

        public string KeyPrefix { get; init; } = string.Empty;

        /// <summary>
        /// Display this only once.
        /// It cannot be recovered later.
        /// </summary>
        public string ApiKey { get; init; } = string.Empty;

        public DateTime? KeyExpiresAt { get; init; }

        public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
    }
}
