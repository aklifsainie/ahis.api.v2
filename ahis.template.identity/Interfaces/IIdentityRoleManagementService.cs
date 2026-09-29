using ahis.template.identity.Models.DTOs;
using FluentResults;

namespace ahis.template.identity.Interfaces;

public interface IIdentityRoleManagementService
{
    Task<Result<IdentityRolePageDto<IdentityRoleListItemDto>>> GetRolesAsync(string? name, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<Result<IdentityRoleDetailDto>> GetRoleAsync(string roleId, CancellationToken cancellationToken);
    Task<Result<IdentityRoleDetailDto>> CreateRoleAsync(string actorUserId, string? proof, string name, CancellationToken cancellationToken);
    Task<Result<IdentityRoleDetailDto>> RenameRoleAsync(string actorUserId, string? proof, string roleId, string? etag, string name, CancellationToken cancellationToken);
    Task<Result> DeleteRoleAsync(string actorUserId, string? proof, string roleId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<IdentityRoleAssignmentDto>>> GetUserRolesAsync(string userId, CancellationToken cancellationToken);
    Task<Result<IdentityRoleAssignmentChangeDto>> ReplaceUserRolesAsync(string actorUserId, string? proof, string userId, IReadOnlyCollection<string>? roleIds, CancellationToken cancellationToken);
    Task<Result<IdentityRolePageDto<IdentityRoleUserDto>>> GetRoleUsersAsync(string roleId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
