using ahis.template.application.Shared.Mediator;
using FluentResults;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Features.AuthenticationFeatures.Queries
{
    public record DecodeTokenQuery(string Token) : IRequest<Result<JwtDecodeDto>>;

    public class JwtDecodeDto
    {
        public Dictionary<string, object> Payload { get; set; } = new();
        public List<JwtClaimDto> Claims { get; set; } = new();
    }

    public class JwtClaimDto
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class DecodeTokenQueryHandler : IRequestHandler<DecodeTokenQuery, Result<JwtDecodeDto>>
    {
        public Task<Result<JwtDecodeDto>> Handle(DecodeTokenQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(request.Token);

                var dto = new JwtDecodeDto
                {
                    Payload = token.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Claims = token.Claims.Select(c => new JwtClaimDto
                    {
                        Type = c.Type,
                        Value = c.Value
                    }).ToList()
                };

                return Task.FromResult(Result.Ok(dto));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result.Fail<JwtDecodeDto>($"Failed to decode token: {ex.Message}"));
            }
        }
    }
}
