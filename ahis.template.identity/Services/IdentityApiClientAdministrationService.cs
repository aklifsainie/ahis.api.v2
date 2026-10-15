using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Security;
using FluentResults;
using Microsoft.AspNetCore.Identity;

namespace ahis.template.identity.Services;

public sealed class IdentityApiClientAdministrationService : IIdentityApiClientAdministrationService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAccountSecurityProofService _proof;
    private readonly IIdentityTokenStateService _tokens;

    public IdentityApiClientAdministrationService(
        UserManager<ApplicationUser> users,
        IAccountSecurityProofService proof,
        IIdentityTokenStateService tokens)
    {
        _users = users;
        _proof = proof;
        _tokens = tokens;
    }

    public async Task<Result> ValidateAsync(string actorUserId, string? proof, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            return Fail("Forbidden");

        var actor = await _users.FindByIdAsync(actorUserId);
        if (!_tokens.IsEligible(actor) || actor is null ||
            !await _users.IsInRoleAsync(actor, IdentityRoleNames.Superadmin))
            return Fail("Forbidden");

        if (!await _proof.IsValidAsync(actorUserId, proof, cancellationToken))
            return Fail("Validation");

        return Result.Ok();
    }

    private static Result Fail(string category) =>
        Result.Fail(new Error("Unable to authorize API-client administration.").WithMetadata(category, true));
}
