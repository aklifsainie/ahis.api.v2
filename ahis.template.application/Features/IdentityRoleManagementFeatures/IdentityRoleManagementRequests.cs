using ahis.template.application.Interfaces.Commons;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Shared;
using ahis.template.application.Shared.Mediator;
using ahis.template.domain.Enums;
using ahis.template.domain.Models.ViewModels.IdentityRoleVM;
using ahis.template.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ahis.template.application.Features.IdentityRoleManagementFeatures;
public sealed class GetRolesQuery : IRequest<Result<PagedResult<List<AdminRoleListItemVM>>>> { public string? Name {get;init;} public int PageNumber {get;init;}=1; public int PageSize {get;init;}=20; }
public sealed class GetRoleQuery : IRequest<Result<AdminRoleDetailVM>> { public string RoleId {get;init;}=string.Empty; }
public sealed class CreateRoleCommand : IRequest<Result<AdminRoleDetailVM>> { public string Name {get;init;}=string.Empty; public string? StepUpProof {get;init;} }
public sealed class RenameRoleCommand : IRequest<Result<AdminRoleDetailVM>> { public string RoleId {get;init;}=string.Empty; public string Name {get;init;}=string.Empty; public string? ETag {get;init;} public string? StepUpProof {get;init;} }
public sealed class DeleteRoleCommand : IRequest<Result> { public string RoleId {get;init;}=string.Empty; public string? StepUpProof {get;init;} }
public sealed class GetUserRolesQuery : IRequest<Result<List<AdminUserRoleVM>>> { public string UserId {get;init;}=string.Empty; }
public sealed class ReplaceUserRolesCommand : IRequest<Result> { public string UserId {get;init;}=string.Empty; public IReadOnlyCollection<string>? RoleIds {get;init;} public string? StepUpProof {get;init;} }
public sealed class GetRoleUsersQuery : IRequest<Result<PagedResult<List<AdminRoleUserVM>>>> { public string RoleId {get;init;}=string.Empty; public int PageNumber {get;init;}=1; public int PageSize {get;init;}=20; }

public sealed class IdentityRoleManagementHandler : IRequestHandler<GetRolesQuery,Result<PagedResult<List<AdminRoleListItemVM>>>>, IRequestHandler<GetRoleQuery,Result<AdminRoleDetailVM>>, IRequestHandler<CreateRoleCommand,Result<AdminRoleDetailVM>>, IRequestHandler<RenameRoleCommand,Result<AdminRoleDetailVM>>, IRequestHandler<DeleteRoleCommand,Result>, IRequestHandler<GetUserRolesQuery,Result<List<AdminUserRoleVM>>>, IRequestHandler<ReplaceUserRolesCommand,Result>, IRequestHandler<GetRoleUsersQuery,Result<PagedResult<List<AdminRoleUserVM>>>>
{ private readonly IIdentityRoleManagementService _service; private readonly ICurrentUserService _current; private readonly IAuditLogger _audit; private readonly ILogger<IdentityRoleManagementHandler> _log;
 public IdentityRoleManagementHandler(IIdentityRoleManagementService service, ICurrentUserService current, IAuditLogger audit, ILogger<IdentityRoleManagementHandler> log){_service=service;_current=current;_audit=audit;_log=log;}
 public async Task<Result<PagedResult<List<AdminRoleListItemVM>>>> Handle(GetRolesQuery r,CancellationToken ct){var x=await _service.GetRolesAsync(r.Name,Math.Max(1,r.PageNumber),Math.Clamp(r.PageSize,1,100),ct);return x.IsFailed?Result.Fail<PagedResult<List<AdminRoleListItemVM>>>(x.Errors):Result.Ok(new PagedResult<List<AdminRoleListItemVM>>(x.Value.Items.Select(v=>new AdminRoleListItemVM(v.Id,v.Name,v.IsProtected,v.AssignmentCount)).ToList(),x.Value.TotalCount,x.Value.PageNumber,x.Value.PageSize));}
 public async Task<Result<AdminRoleDetailVM>> Handle(GetRoleQuery r,CancellationToken ct)=>MapDetail(await _service.GetRoleAsync(r.RoleId,ct));
 public async Task<Result<AdminRoleDetailVM>> Handle(CreateRoleCommand r,CancellationToken ct)=>await Change(async()=>await _service.CreateRoleAsync(_current.UserId??string.Empty,r.StepUpProof,r.Name,ct),"RoleCreated",r.Name);
 public async Task<Result<AdminRoleDetailVM>> Handle(RenameRoleCommand r,CancellationToken ct)=>await Change(async()=>await _service.RenameRoleAsync(_current.UserId??string.Empty,r.StepUpProof,r.RoleId,r.ETag,r.Name,ct),"RoleRenamed",r.RoleId);
 public async Task<Result> Handle(DeleteRoleCommand r,CancellationToken ct)=>await ChangeVoid(async()=>await _service.DeleteRoleAsync(_current.UserId??string.Empty,r.StepUpProof,r.RoleId,ct),"RoleDeleted",r.RoleId);
 public async Task<Result<List<AdminUserRoleVM>>> Handle(GetUserRolesQuery r,CancellationToken ct){var x=await _service.GetUserRolesAsync(r.UserId,ct);return x.IsFailed?Result.Fail<List<AdminUserRoleVM>>(x.Errors):Result.Ok(x.Value.Select(v=>new AdminUserRoleVM(v.Id,v.Name,v.IsProtected)).ToList());}
 public async Task<Result> Handle(ReplaceUserRolesCommand r,CancellationToken ct)=>await ChangeVoid(async()=>await _service.ReplaceUserRolesAsync(_current.UserId??string.Empty,r.StepUpProof,r.UserId,r.RoleIds,ct),"UserRolesReplaced",r.UserId);
 public async Task<Result<PagedResult<List<AdminRoleUserVM>>>> Handle(GetRoleUsersQuery r,CancellationToken ct){var x=await _service.GetRoleUsersAsync(r.RoleId,Math.Max(1,r.PageNumber),Math.Clamp(r.PageSize,1,100),ct);return x.IsFailed?Result.Fail<PagedResult<List<AdminRoleUserVM>>>(x.Errors):Result.Ok(new PagedResult<List<AdminRoleUserVM>>(x.Value.Items.Select(v=>new AdminRoleUserVM(v.UserId,v.Username)).ToList(),x.Value.TotalCount,x.Value.PageNumber,x.Value.PageSize));}
 private static Result<AdminRoleDetailVM> MapDetail(FluentResults.Result<ahis.template.identity.Models.DTOs.IdentityRoleDetailDto> x)=>x.IsFailed?Result.Fail<AdminRoleDetailVM>(x.Errors):Result.Ok(new AdminRoleDetailVM(x.Value.Id,x.Value.Name,x.Value.IsProtected,x.Value.AssignmentCount,[],x.Value.ETag));
 private async Task<Result<AdminRoleDetailVM>> Change(Func<Task<FluentResults.Result<ahis.template.identity.Models.DTOs.IdentityRoleDetailDto>>> work,string action,string id){var x=await work();if(x.IsFailed)return MapDetail(x);await Audit(action,id);return MapDetail(x);}
 private async Task<Result> ChangeVoid(Func<Task<FluentResults.Result>> work,string action,string id){var x=await work();if(x.IsSuccess)await Audit(action,id);return x;}
 private async Task Audit(string action,string id){try{await _audit.LogAsync(AuditActionEnum.Update,"IdentityRoleManagement",id,action,cancellationToken:CancellationToken.None);}catch(Exception ex){_log.LogWarning(ex,"Audit delivery failed after {Action}",action);}}
}
