using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared.Errors;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ahis.template.application.Features.AccountFeatures.Commands;

public sealed class DisableAdminUserCommand : IRequest<Result<AdminUserControlOutcome>>
{
    public string UserId { get; init; } = string.Empty;
    public string? StepUpProof { get; init; }
}

public sealed class EnableAdminUserCommand : IRequest<Result<AdminUserControlOutcome>>
{
    public string UserId { get; init; } = string.Empty;
    public string? StepUpProof { get; init; }
}

public sealed class ResetAdminUserMfaCommand : IRequest<Result<AdminUserControlOutcome>>
{
    public string UserId { get; init; } = string.Empty;
    public string? StepUpProof { get; init; }
}

public sealed class RequireAdminUserPasswordResetCommand : IRequest<Result<AdminUserControlOutcome>>
{
    public string UserId { get; init; } = string.Empty;
    public string? StepUpProof { get; init; }
}

public sealed class AdminUserAccessCommandHandler : IRequestHandler<DisableAdminUserCommand, Result<AdminUserControlOutcome>>, IRequestHandler<EnableAdminUserCommand, Result<AdminUserControlOutcome>>, IRequestHandler<ResetAdminUserMfaCommand, Result<AdminUserControlOutcome>>, IRequestHandler<RequireAdminUserPasswordResetCommand, Result<AdminUserControlOutcome>>
{
    private readonly IAccountService _accounts;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly ILogger<AdminUserAccessCommandHandler> _logger;

    public AdminUserAccessCommandHandler(IAccountService accounts, ICurrentUserService current, IAuditLogger audit, ILogger<AdminUserAccessCommandHandler> logger)
    { _accounts = accounts; _current = current; _audit = audit; _logger = logger; }

    public Task<Result<AdminUserControlOutcome>> Handle(DisableAdminUserCommand request, CancellationToken cancellationToken) =>
        Execute(request.UserId, request.StepUpProof, "AdminUserDisabled", (actor, target, proof) => _accounts.DisableAdminUserAsync(actor, target, proof, cancellationToken));

    public Task<Result<AdminUserControlOutcome>> Handle(EnableAdminUserCommand request, CancellationToken cancellationToken) =>
        Execute(request.UserId, request.StepUpProof, "AdminUserEnabled", (actor, target, proof) => _accounts.EnableAdminUserAsync(actor, target, proof, cancellationToken));

    public Task<Result<AdminUserControlOutcome>> Handle(ResetAdminUserMfaCommand request, CancellationToken cancellationToken) =>
        Execute(request.UserId, request.StepUpProof, "AdminUserMfaReset", (actor, target, proof) => _accounts.ResetAdminUserMfaAsync(actor, target, proof, cancellationToken));

    public Task<Result<AdminUserControlOutcome>> Handle(RequireAdminUserPasswordResetCommand request, CancellationToken cancellationToken) =>
        Execute(request.UserId, request.StepUpProof, "AdminUserPasswordResetRequired", (actor, target, proof) => _accounts.RequireAdminUserPasswordResetAsync(actor, target, proof, cancellationToken));

    private async Task<Result<AdminUserControlOutcome>> Execute(string target, string? proof, string action,
        Func<string, string, string?, Task<Result<AdminUserControlOutcome>>> operation)
    {
        var actor = _current.UserId;
        if (string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(proof) || actor == target)
            return Result.Fail<AdminUserControlOutcome>(new ValidationError("Unable to complete administrative account change."));
        var result = await operation(actor, target, proof);
        if (result.IsFailed || result.Value != AdminUserControlOutcome.TargetNotFound)
        {
            if (result.IsSuccess && result.Value == AdminUserControlOutcome.Changed)
            {
                try { await _audit.LogAsync(AuditActionEnum.StatusChange, "AccountSecurity", target, action, cancellationToken: CancellationToken.None); }
                catch (Exception ex) { _logger.LogWarning(ex, "Audit delivery failed after administrative account change for target user {TargetUserId}", target); }
            }
            return result;
        }
        return Result.Fail<AdminUserControlOutcome>(new EntityNotFoundError("IdentityUser", target));
    }
}
