# Authentication Feature Instructions

Authentication owns account-state discovery, password login, 2FA completion, JWT issuance, refresh-token rotation, logout, password reset, and token diagnostics.

Controllers normally dispatch custom mediator requests; handlers delegate multi-step work to `IAuthenticationService`, which coordinates Identity managers, JWTs, refresh-token persistence, email, and transactions. Preserve that service boundary.

Before changing this module, inspect the affected controller action, request/handler/service method, `ApplicationUser`, `RefreshToken`, `IdentityContext`, cookie handling, and JWT/Identity setup in `Program.cs`. Read [the Authentication module guide](../../../docs/modules/authentication.md) and Account guidance for password, email, or 2FA setup changes.

Observed risks to retain: password login's active/deleted check is not also enforced for existing JWTs or refresh; the 2FA handler requires a current user although password login issues no token when 2FA is required; refresh-cookie paths differ across actions; security-stamp updates are not checked during JWT or refresh validation. Do not describe these as desired behavior or silently copy them.
