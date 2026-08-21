using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM;
using FluentResults;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.ApiKeyAuthenticationFeatures.Commands
{
    public class CreateApiClientCommand : IRequest<Result<CreateApiClientResponseVM>>
    {
        [Required]
        [MaxLength(100)]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ClientName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? ContactName { get; set; }

        [EmailAddress]
        [MaxLength(320)]
        public string? ContactEmail { get; set; }

        [Range(1, 10000)]
        public int RateLimitPerMinute { get; set; } = 60;

        [Required]
        [MaxLength(100)]
        public string InitialKeyName { get; set; } = "Initial production key";

        public DateTime? KeyExpiresAt { get; set; }

        public List<string> Permissions { get; set; } = [];
    }
}
