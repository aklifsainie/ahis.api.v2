using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Errors;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ahis.template.application.Features.AccountFeatures.Commands;

public sealed class UnlockAdminUserCommand : IRequest<Result<AdminUserUnlockOutcome>>
{
    public string UserId { get; init; } = string.Empty;
    public string? StepUpProof { get; init; }
}

public sealed class UnlockAdminUserCommandHandler : IRequestHandler<UnlockAdminUserCommand, Result<AdminUserUnlockOutcome>>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UnlockAdminUserCommandHandler> _logger;

    public UnlockAdminUserCommandHandler(IAccountService accountService, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<UnlockAdminUserCommandHandler> logger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result<AdminUserUnlockOutcome>> Handle(UnlockAdminUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId) || string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.StepUpProof) || _currentUser.UserId == request.UserId)
            return Result.Fail<AdminUserUnlockOutcome>(new ValidationError("Unable to unlock user."));

        var result = await _accountService.UnlockAdminUserAsync(_currentUser.UserId, request.UserId, request.StepUpProof, cancellationToken);
        if (result.IsFailed || result.Value != AdminUserUnlockOutcome.Unlocked)
        {
            return result.IsSuccess && result.Value == AdminUserUnlockOutcome.TargetNotFound
                ? Result.Fail<AdminUserUnlockOutcome>(new EntityNotFoundError("IdentityUser", request.UserId))
                : result;
        }

        try
        {
            await _auditLogger.LogAsync(AuditActionEnum.StatusChange, "AccountSecurity", request.UserId, "AdminUserUnlocked", cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit delivery failed after administrative user unlock for target user {TargetUserId}", request.UserId);
        }

        return result;
    }
}
