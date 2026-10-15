# Authentication Feature Instructions

Authentication owns account-state discovery, password login, 2FA completion, JWT issuance, refresh-token rotation, logout, password reset, and token diagnostics.

Controllers normally dispatch custom mediator requests; handlers delegate multi-step work to `IAuthenticationService`, which coordinates Identity managers, JWTs, refresh-token persistence, email, and transactions. Preserve that service boundary.

Before changing this module, inspect the affected controller action, request/handler/service method, `ApplicationUser`, `RefreshToken`, `IdentityContext`, cookie handling, and JWT/Identity setup in `Program.cs`. Read [the Authentication module guide](../../../docs/modules/authentication.md) and Account guidance for password, email, or 2FA setup changes.

Observed risks to retain: the 2FA handler requires a current user although password login issues no token when 2FA is required; refresh-cookie paths differ across actions. JWT and refresh validation now enforce account state and a SecurityStamp-derived version, but the Identity migration must be approved and generated before deployment. Do not describe the remaining risks as desired behavior or silently copy them.
