namespace ahis.template.domain.Models.ViewModels.IdentityRoleVM;
public sealed record AdminRoleListItemVM(string Id, string Name, bool IsProtected, int AssignmentCount);
public sealed record AdminRoleDetailVM(string Id, string Name, bool IsProtected, int AssignmentCount, IReadOnlyList<string> Permissions, string ETag);
public sealed record AdminUserRoleVM(string Id, string Name, bool IsProtected);
public sealed record AdminRoleUserVM(string UserId, string Username);
