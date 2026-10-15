using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Mediator;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.DTOs;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace ahis.template.application.Features.AccountFeatures.Commands;

public sealed class PasskeyRegistrationOptionsCommand : IRequest<Result<PasskeyOptionsDto>> { }
public sealed class CompletePasskeyRegistrationCommand : IRequest<Result<PasskeyCredentialDto>>
{
    public string DisplayName { get; init; } = string.Empty;
    public string StepUpProof { get; init; } = string.Empty;
    public string? Password { get; init; }
    public string? RecoveryCode { get; init; }
    public string AttestationResponseJson { get; init; } = string.Empty;
    public string CeremonyId { get; init; } = string.Empty;
}
public sealed class ListPasskeysQuery : IRequest<Result<IReadOnlyList<PasskeyCredentialDto>>> { }
public sealed class DeletePasskeyCommand : IRequest<Result>
{
    public Guid CredentialId { get; init; }
    public string? StepUpProof { get; init; }
    public string? Password { get; init; }
    public string? RecoveryCode { get; init; }
}

public sealed class PasskeyAccountHandler : IRequestHandler<PasskeyRegistrationOptionsCommand, Result<PasskeyOptionsDto>>,
    IRequestHandler<CompletePasskeyRegistrationCommand, Result<PasskeyCredentialDto>>,
    IRequestHandler<ListPasskeysQuery, Result<IReadOnlyList<PasskeyCredentialDto>>>,
    IRequestHandler<DeletePasskeyCommand, Result>
{
    private readonly IPasskeyService _passkeys;
    private readonly ICurrentUserService _current;
    private readonly IHttpContextAccessor _http;
    private readonly IAuditLogger _audit;
    public PasskeyAccountHandler(IPasskeyService passkeys, ICurrentUserService current, IHttpContextAccessor http, IAuditLogger audit)
    { _passkeys = passkeys; _current = current; _http = http; _audit = audit; }
    public Task<Result<PasskeyOptionsDto>> Handle(PasskeyRegistrationOptionsCommand request, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(_current.UserId) ? Task.FromResult(Result.Fail<PasskeyOptionsDto>("Passkey operation failed.")) :
        _passkeys.RegistrationOptionsAsync(_current.UserId, _http.HttpContext?.Connection.RemoteIpAddress?.ToString(), ct);
    public async Task<Result<PasskeyCredentialDto>> Handle(CompletePasskeyRegistrationCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_current.UserId)) return Result.Fail<PasskeyCredentialDto>("Passkey operation failed.");
        var result = await _passkeys.CompleteRegistrationAsync(_current.UserId,
            new PasskeyRegistrationDto(request.DisplayName, request.StepUpProof, request.Password, request.RecoveryCode, request.AttestationResponseJson, request.CeremonyId), ct);
        if (result.IsSuccess) await _audit.LogAsync(ahis.template.domain.Enums.AuditActionEnum.Update, "AccountSecurity", _current.UserId, "PasskeyAdded", cancellationToken: ct);
        return result;
    }
    public Task<Result<IReadOnlyList<PasskeyCredentialDto>>> Handle(ListPasskeysQuery request, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(_current.UserId) ? Task.FromResult(Result.Fail<IReadOnlyList<PasskeyCredentialDto>>("Passkey operation failed.")) : _passkeys.ListAsync(_current.UserId, ct);
    public async Task<Result> Handle(DeletePasskeyCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_current.UserId)) return Result.Fail("Unable to remove passkey.");
        var result = await _passkeys.DeleteAsync(_current.UserId, request.CredentialId, request.StepUpProof, request.Password, request.RecoveryCode, ct);
        if (result.IsSuccess) await _audit.LogAsync(ahis.template.domain.Enums.AuditActionEnum.Update, "AccountSecurity", _current.UserId, "PasskeyRemoved", cancellationToken: ct);
        return result;
    }
}
