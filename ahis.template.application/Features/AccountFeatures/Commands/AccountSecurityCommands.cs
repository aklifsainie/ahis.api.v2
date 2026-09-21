using ahis.template.application.Interfaces.Services;
using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentResults;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.application.Features.AccountFeatures.Commands;

public sealed class ReauthenticateCommand : IRequest<Result<string>>
{
    [Required]
    public string Password { get; set; } = default!;
    public string? TwoFactorCode { get; set; }
}

public sealed class ResetAuthenticatorCommand : IRequest<Result>
{
    [Required]
    public string StepUpProof { get; set; } = default!;
}

public sealed class RevokeAllSessionsCommand : IRequest<Result>
{
    [Required]
    public string StepUpProof { get; set; } = default!;
}

public sealed class RevokeSessionCommand : IRequest<Result<bool>>
{
    public Guid SessionId { get; init; }
    public string? StepUpProof { get; init; }
}

public sealed class RequestEmailChangeCommand : IRequest<Result>
{
    [Required, EmailAddress]
    public string NewEmail { get; set; } = default!;
    [Required, Url]
    public string CallbackBaseUrl { get; set; } = default!;
    [Required]
    public string StepUpProof { get; set; } = default!;
}

public sealed class ConfirmEmailChangeCommand : IRequest<Result>
{
    [Required]
    public string UserId { get; set; } = default!;
    [Required, EmailAddress]
    public string NewEmail { get; set; } = default!;
    [Required]
    public string Token { get; set; } = default!;
}

public sealed class DeactivateAccountCommand : IRequest<Result>
{
    public bool Confirmation { get; set; }
    [Required]
    public string StepUpProof { get; set; } = default!;
}

public sealed class ReauthenticateCommandHandler : IRequestHandler<ReauthenticateCommand, Result<string>>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public ReauthenticateCommandHandler(IAccountService accountService, ICurrentUserService currentUser, IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result<string>> Handle(ReauthenticateCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Fail<string>("Re-authentication failed.");

        var result = await _accountService.ReauthenticateAsync(_currentUser.UserId, request.Password, request.TwoFactorCode, cancellationToken);
        await _auditLogger.LogAsync(
            result.IsSuccess ? AuditActionEnum.Login : AuditActionEnum.LoginFailed,
            "AccountSecurity",
            _currentUser.UserId,
            result.IsSuccess ? "StepUpAuthentication" : "StepUpAuthenticationFailed",
            cancellationToken: cancellationToken);
        return result;
    }
}

public sealed class ResetAuthenticatorCommandHandler : IRequestHandler<ResetAuthenticatorCommand, Result>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public ResetAuthenticatorCommandHandler(IAccountService accountService, ICurrentUserService currentUser, IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(ResetAuthenticatorCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Fail("Unable to reset authenticator.");

        var result = await _accountService.ResetAuthenticatorAsync(_currentUser.UserId, request.StepUpProof, cancellationToken);
        if (result.IsSuccess)
            await _auditLogger.LogAsync(AuditActionEnum.Update, "AccountSecurity", _currentUser.UserId, "AuthenticatorReset", cancellationToken: cancellationToken);
        return result;
    }
}

public sealed class RevokeAllSessionsCommandHandler : IRequestHandler<RevokeAllSessionsCommand, Result>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public RevokeAllSessionsCommandHandler(IAccountService accountService, ICurrentUserService currentUser, IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(RevokeAllSessionsCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Fail("Unable to revoke sessions.");

        var result = await _accountService.RevokeAllSessionsAsync(
            _currentUser.UserId,
            request.StepUpProof,
            cancellationToken);
        await _auditLogger.LogAsync(
            result.IsSuccess ? AuditActionEnum.Logout : AuditActionEnum.LoginFailed,
            "AccountSecurity",
            _currentUser.UserId,
            result.IsSuccess ? "AllSessionsRevoked" : "AllSessionsRevocationFailed",
            cancellationToken: cancellationToken);
        return result;
    }
}

public sealed class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, Result<bool>>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public RevokeSessionCommandHandler(
        IAccountService accountService,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result<bool>> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Fail<bool>("Unable to revoke session.");

        var isCurrentSession = _currentUser.SessionId == request.SessionId;
        var result = await _accountService.RevokeSessionAsync(
            _currentUser.UserId,
            request.SessionId,
            _currentUser.SessionId,
            request.StepUpProof,
            cancellationToken);

        await _auditLogger.LogAsync(
            result.IsSuccess ? AuditActionEnum.Logout : AuditActionEnum.LoginFailed,
            "AccountSecurity",
            _currentUser.UserId,
            result.IsSuccess ? "SessionRevoked" : "SessionRevocationFailed",
            cancellationToken: cancellationToken);

        return result.IsSuccess
            ? Result.Ok(isCurrentSession)
            : Result.Fail<bool>(result.Errors);
    }
}

public sealed class RequestEmailChangeCommandHandler : IRequestHandler<RequestEmailChangeCommand, Result>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public RequestEmailChangeCommandHandler(IAccountService accountService, ICurrentUserService currentUser, IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(RequestEmailChangeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Fail("Unable to process email change.");

        var result = await _accountService.RequestEmailChangeAsync(
            _currentUser.UserId,
            request.NewEmail,
            request.CallbackBaseUrl,
            request.StepUpProof,
            cancellationToken);
        if (result.IsSuccess)
            await _auditLogger.LogAsync(AuditActionEnum.Update, "AccountSecurity", _currentUser.UserId, "EmailChangeRequested", cancellationToken: cancellationToken);
        return result;
    }
}

public sealed class ConfirmEmailChangeCommandHandler : IRequestHandler<ConfirmEmailChangeCommand, Result>
{
    private readonly IAccountService _accountService;
    private readonly IAuditLogger _auditLogger;

    public ConfirmEmailChangeCommandHandler(IAccountService accountService, IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var result = await _accountService.ConfirmEmailChangeAsync(request.UserId, request.NewEmail, request.Token, cancellationToken);
        if (result.IsSuccess)
            await _auditLogger.LogAsync(AuditActionEnum.Update, "AccountSecurity", request.UserId, "EmailChanged", overrideUserId: request.UserId, cancellationToken: cancellationToken);
        return result;
    }
}

public sealed class DeactivateAccountCommandHandler : IRequestHandler<DeactivateAccountCommand, Result>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public DeactivateAccountCommandHandler(IAccountService accountService, ICurrentUserService currentUser, IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Fail("Unable to deactivate account.");

        var result = await _accountService.DeactivateAsync(
            _currentUser.UserId,
            request.Confirmation,
            request.StepUpProof,
            cancellationToken);
        if (result.IsSuccess)
            await _auditLogger.LogAsync(AuditActionEnum.StatusChange, "AccountSecurity", _currentUser.UserId, "AccountDeactivated", cancellationToken: cancellationToken);
        return result;
    }
}
