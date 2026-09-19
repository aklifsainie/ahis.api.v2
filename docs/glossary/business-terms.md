# Business Terms

- **Account**: An `ApplicationUser` and its registration, profile, email, password, and 2FA configuration.
- **Account configured**: `ApplicationUser.IsAccountConfigured`; currently set when profile update requests mark configuration complete.
- **Authentication**: Credential and token workflows that establish or renew a user's identity.
- **Access token**: Short-lived JWT returned in the response body after completed authentication.
- **Refresh token**: Longer-lived random token stored in Identity persistence and normally transported in an HttpOnly secure cookie.
- **Refresh-token rotation**: Revoking the used refresh token and issuing a replacement.
- **API client**: An external system identity represented by `ApiClient`.
- **API key**: A generated credential whose raw value is shown once and whose SHA-256 hash is stored.
- **Permission code**: Lowercase capability string attached to an API client and emitted as an API-key claim.
- **Country**: Active/reference-data record with names and unique alpha-2, alpha-3, and numeric ISO codes.
- **Soft delete**: Setting `IsDelete=true` rather than physically deleting a `BaseEntity`; Country deletion also sets `IsActive=false`.
- **Audit log**: Immutable-style record of who performed an action, on what entity, when, and with what changed/request context.
- **Custom mediator**: The repository's own request dispatcher under `application/Shared/Mediator`; it is not the MediatR package.
