using ahis.template.domain.Models.ViewModels.ApiKeyAuthenticationVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Interfaces.Validators
{
    public interface IApiKeyValidator
    {
        Task<ApiKeyValidationResult> ValidateAsync(string rawApiKey, CancellationToken cancellationToken = default);

        Task UpdateLastUsedAsync(Guid apiClientKeyId, CancellationToken cancellationToken = default);
    }
}
