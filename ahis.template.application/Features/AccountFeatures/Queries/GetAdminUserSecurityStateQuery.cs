using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Shared.Errors;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.ViewModels.AccountVM;
using ahis.template.identity.Interfaces;
using FluentResults;

namespace ahis.template.application.Features.AccountFeatures.Queries;

public sealed class GetAdminUserSecurityStateQuery : IRequest<Result<AdminUserSecurityStateResponseVM>>
{
    public string UserId { get; set; } = string.Empty;
}

public sealed class GetAdminUserSecurityStateQueryHandler
    : IRequestHandler<GetAdminUserSecurityStateQuery, Result<AdminUserSecurityStateResponseVM>>
{
    private readonly IAccountService _accountService;
    private readonly IAuditLogger _auditLogger;

    public GetAdminUserSecurityStateQueryHandler(IAccountService accountService, IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<AdminUserSecurityStateResponseVM>> Handle(
        GetAdminUserSecurityStateQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return Result.Fail<AdminUserSecurityStateResponseVM>("A user ID is required.");

        var result = await _accountService.GetAdminUserSecurityStateAsync(request.UserId, cancellationToken);
        if (result.IsFailed)
        {
            if (result.Errors.Any(error => error.Message == "User not found."))
                return Result.Fail<AdminUserSecurityStateResponseVM>(new EntityNotFoundError("IdentityUser", request.UserId));

            return Result.Fail<AdminUserSecurityStateResponseVM>(result.Errors);
        }

        var response = new AdminUserSecurityStateResponseVM
        {
            UserId = result.Value.UserId,
            IsActive = result.Value.IsActive,
            IsDeleted = result.Value.IsDeleted,
            IsLockedOut = result.Value.IsLockedOut,
            EmailConfirmed = result.Value.EmailConfirmed,
            PhoneConfirmed = result.Value.PhoneConfirmed,
            PasswordPresent = result.Value.PasswordPresent,
            TwoFactorEnabled = result.Value.TwoFactorEnabled,
            AuthenticatorConfigured = result.Value.AuthenticatorConfigured,
            RemainingRecoveryCodeCount = result.Value.RemainingRecoveryCodeCount,
            ActiveSessionCount = result.Value.ActiveSessionCount
        };

        await _auditLogger.LogAsync(
            AuditActionEnum.View,
            "AccountSecurity",
            request.UserId,
            "AdminSecurityStateViewed",
            cancellationToken: cancellationToken);

        return Result.Ok(response);
    }
}
