# API return

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "traceId": "00-424fed9706e23a91602e6fa78402d66e-4a71222146de1bc1-00"
}
```

# Internal Error

Error location: `ahis.template.identity/Services/AccountService.cs:478`

```text
System.MissingMethodException
  HResult=0x80131513
  Message=Method not found: 'System.Threading.Tasks.Task`1<Int32> Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync(System.Linq.IQueryable`1<!!0>, System.Linq.Expressions.Expression`1<System.Func`2<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls`1<!!0>,Microsoft.EntityFrameworkCore.Query.SetPropertyCalls`1<!!0>>>, System.Threading.CancellationToken)'.
  Source=ahis.template.identity
  StackTrace:
   at ahis.template.identity.Services.IdentityTokenStateService.<InvalidateAsync>d__7.MoveNext() in D:\AHIS\AhisApiTemplate\ahis.template.identity\Services\IdentityTokenStateService.cs:line 73
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](TStateMachine& stateMachine)
   at ahis.template.identity.Services.IdentityTokenStateService.InvalidateAsync(ApplicationUser user, CancellationToken cancellationToken) in D:\AHIS\AhisApiTemplate\ahis.template.identity\Services\IdentityTokenStateService.cs:line 54
   at ahis.template.identity.Services.AccountService.<RevokeAllSessionsAsync>d__19.MoveNext() in D:\AHIS\AhisApiTemplate\ahis.template.identity\Services\AccountService.cs:line 478

  This exception was originally thrown at this call stack:
    [External Code]
    ahis.template.identity.Services.AccountService.RevokeAllSessionsAsync(string, string, System.Threading.CancellationToken) in AccountService.cs
```
