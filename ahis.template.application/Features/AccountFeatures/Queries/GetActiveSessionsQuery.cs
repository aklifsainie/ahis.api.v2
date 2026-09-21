using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.ViewModels.AccountVM;
using ahis.template.identity.Interfaces;
using FluentResults;

namespace ahis.template.application.Features.AccountFeatures.Queries;

public sealed class GetActiveSessionsQuery : IRequest<Result<PagedResult<List<ActiveSessionResponseVM>>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class GetActiveSessionsQueryHandler : IRequestHandler<GetActiveSessionsQuery, Result<PagedResult<List<ActiveSessionResponseVM>>>>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public GetActiveSessionsQueryHandler(
        IAccountService accountService,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger)
    {
        _accountService = accountService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result<PagedResult<List<ActiveSessionResponseVM>>>> Handle(
        GetActiveSessionsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Fail<PagedResult<List<ActiveSessionResponseVM>>>("Unable to retrieve sessions.");

        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var result = await _accountService.GetActiveSessionsAsync(
            _currentUser.UserId,
            _currentUser.SessionId,
            pageNumber,
            pageSize,
            cancellationToken);

        if (result.IsFailed)
            return Result.Fail<PagedResult<List<ActiveSessionResponseVM>>>(result.Errors);

        var response = new PagedResult<List<ActiveSessionResponseVM>>(
            result.Value.Sessions.Select(session => new ActiveSessionResponseVM
            {
                SessionId = session.SessionId,
                CreatedAt = session.CreatedAt,
                LastUsedAt = session.LastUsedAt,
                ExpiresAt = session.ExpiresAt,
                IsCurrent = session.IsCurrent
            }).ToList(),
            result.Value.TotalCount,
            pageNumber,
            pageSize);

        await _auditLogger.LogAsync(
            AuditActionEnum.View,
            "AccountSecurity",
            "Sessions",
            $"ActiveSessionCount={response.TotalCount}",
            cancellationToken: cancellationToken);

        return Result.Ok(response);
    }
}
