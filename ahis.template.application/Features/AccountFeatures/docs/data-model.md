# Account Data Model

`ApplicationUser` extends ASP.NET Core `IdentityUser` with names, date of birth, account-configuration state, email verification timestamp, authenticator material, recovery codes, 2FA activation timestamp, active/deleted flags, and created/updated timestamps.

Identity's own properties hold email confirmation, password hash, phone, two-factor flag, lockout state, claims, and roles. Identity DTOs are used internally by `IAccountService`; Application handlers map selected responses to Domain ViewModels.

Account data is stored by `IdentityContext`, not `ApplicationDbContext`.
