namespace ahis.template.identity.Models.DTOs;

public sealed record IdentityRoleListItemDto(string Id, string Name, bool IsProtected, int AssignmentCount);
public sealed record IdentityRoleDetailDto(string Id, string Name, bool IsProtected, int AssignmentCount, IReadOnlyList<string> Permissions, string ETag);
public sealed record IdentityRoleAssignmentDto(string Id, string Name, bool IsProtected);
public sealed record IdentityRoleAssignmentChangeDto(bool Changed, IReadOnlyList<string> AddedRoleIds, IReadOnlyList<string> RemovedRoleIds);
public sealed record IdentityRolePermissionChangeDto(bool Changed, IReadOnlyList<string> AddedPermissions, IReadOnlyList<string> RemovedPermissions, string ETag);
public sealed record IdentityRoleUserDto(string UserId, string Username);
public sealed record IdentityRolePageDto<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);
