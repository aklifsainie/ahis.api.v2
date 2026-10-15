using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Errors;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ahis.template.application.Features.AccountFeatures.Commands;

public sealed class RevokeAdminUserSessionsCommand : IRequest<Result<bool>>
{
    public string UserId { get; init; } = string.Empty;
    public string? StepUpProof { get; init; }
}

public sealed class RevokeAdminUserSessionsCommandHandler
    : IRequestHandler<RevokeAdminUserSessionsCommand, Result<bool>>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<RevokeAdminUserSessionsCommandHandler> _logger;

    public RevokeAdminUserSessionsCommandHandler(
        IAccountService accountService,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        ILogger<RevokeAdminUserSessionsCommandHandler> logger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        RevokeAdminUserSessionsCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId) ||
            string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.StepUpProof))
        {
            return Result.Fail<bool>(new ValidationError("Unable to revoke sessions."));
        }

        var result = await _accountService.RevokeAdminUserSessionsAsync(
            _currentUser.UserId,
            request.UserId,
            request.StepUpProof,
            cancellationToken);
        if (result.IsFailed)
            return result;

        if (!result.Value)
            return Result.Fail<bool>(new EntityNotFoundError("IdentityUser", request.UserId));

        try
        {
            await _auditLogger.LogAsync(
                AuditActionEnum.StatusChange,
                "AccountSecurity",
                request.UserId,
                "AdminSessionsRevoked",
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit delivery failed after administrative session revocation for target user {TargetUserId}", request.UserId);
        }

        return result;
    }
}
