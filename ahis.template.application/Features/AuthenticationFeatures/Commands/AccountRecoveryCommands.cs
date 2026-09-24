using System.ComponentModel.DataAnnotations;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentResults;

namespace ahis.template.application.Features.AuthenticationFeatures.Commands;

public sealed class StartAccountRecoveryCommand : IRequest<Result>
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;
}

public sealed class CompleteAccountRecoveryCommand : IRequest<Result>
{
    [Required]
    public string Challenge { get; set; } = default!;
    [Required]
    public string NewPassword { get; set; } = default!;
    public TwoFactorProviderEnum? TwoFactorProvider { get; set; }
    public string? TwoFactorCode { get; set; }
}

public sealed class StartAccountRecoveryCommandHandler : IRequestHandler<StartAccountRecoveryCommand, Result>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuditLogger _auditLogger;

    public StartAccountRecoveryCommandHandler(IAuthenticationService authenticationService, IAuditLogger auditLogger)
    {
        _authenticationService = authenticationService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(StartAccountRecoveryCommand request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.StartAccountRecoveryAsync(request.Email, cancellationToken);
        await _auditLogger.LogAsync(
            result.IsSuccess ? AuditActionEnum.Update : AuditActionEnum.LoginFailed,
            "AccountRecovery",
            "pre-auth",
            result.IsSuccess ? "RecoveryStarted" : "RecoveryStartFailed",
            cancellationToken: cancellationToken);
        return result;
    }
}

public sealed class CompleteAccountRecoveryCommandHandler : IRequestHandler<CompleteAccountRecoveryCommand, Result>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuditLogger _auditLogger;

    public CompleteAccountRecoveryCommandHandler(IAuthenticationService authenticationService, IAuditLogger auditLogger)
    {
        _authenticationService = authenticationService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(CompleteAccountRecoveryCommand request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.CompleteAccountRecoveryAsync(
            request.Challenge, request.NewPassword, request.TwoFactorProvider, request.TwoFactorCode, cancellationToken);
        await _auditLogger.LogAsync(
            result.IsSuccess ? AuditActionEnum.Update : AuditActionEnum.LoginFailed,
            "AccountRecovery",
            "pre-auth",
            result.IsSuccess ? "RecoveryCompleted" : "RecoveryCompletionFailed",
            cancellationToken: cancellationToken);
        return result;
    }
}
