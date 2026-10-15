using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.DTOs;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace ahis.template.application.Features.AuthenticationFeatures.Commands;

public sealed class PasskeyAuthenticationOptionsCommand : IRequest<Result<PasskeyOptionsDto>> { }
public sealed class CompletePasskeyAuthenticationCommand : IRequest<Result<AuthenticationResponseVM>>
{
    public string AssertionResponseJson { get; init; } = string.Empty;
    public string CeremonyId { get; init; } = string.Empty;
}
public sealed class PasskeyAuthenticationHandler : IRequestHandler<PasskeyAuthenticationOptionsCommand, Result<PasskeyOptionsDto>>,
    IRequestHandler<CompletePasskeyAuthenticationCommand, Result<AuthenticationResponseVM>>
{
    private readonly IPasskeyService _passkeys;
    private readonly IHttpContextAccessor _http;
    private readonly IAuditLogger _audit;
    public PasskeyAuthenticationHandler(IPasskeyService passkeys, IHttpContextAccessor http, IAuditLogger audit)
    { _passkeys = passkeys; _http = http; _audit = audit; }
    public Task<Result<PasskeyOptionsDto>> Handle(PasskeyAuthenticationOptionsCommand request, CancellationToken ct) =>
        _passkeys.AuthenticationOptionsAsync(_http.HttpContext?.Connection.RemoteIpAddress?.ToString(), ct);
    public async Task<Result<AuthenticationResponseVM>> Handle(CompletePasskeyAuthenticationCommand request, CancellationToken ct)
    {
        var result = await _passkeys.CompleteAuthenticationAsync(
            new PasskeyAssertionDto(request.AssertionResponseJson, request.CeremonyId), _http.HttpContext?.Connection.RemoteIpAddress?.ToString(), ct);
        await _audit.LogAsync(result.IsSuccess ? ahis.template.domain.Enums.AuditActionEnum.Login : ahis.template.domain.Enums.AuditActionEnum.LoginFailed,
            "Authentication", result.IsSuccess ? result.Value.UserId : "pre-auth", result.IsSuccess ? "PasskeyAuthentication" : "PasskeyAuthenticationFailed",
            overrideUserId: result.IsSuccess ? result.Value.UserId : null, cancellationToken: ct);
        return result;
    }
}
