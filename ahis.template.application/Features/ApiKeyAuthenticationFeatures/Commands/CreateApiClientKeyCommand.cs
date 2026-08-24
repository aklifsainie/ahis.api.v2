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
    public class CreateApiClientKeyCommand: IRequest<Result<CreateApiClientKeyResponseVM>>
    {
        [Required]
        [MaxLength(100)]
        public string KeyName { get; set; } = string.Empty;

        public DateTime? ExpiresAt { get; set; }
    }
}
