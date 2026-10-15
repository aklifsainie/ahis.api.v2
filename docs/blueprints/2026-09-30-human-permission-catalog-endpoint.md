# Blueprint: Human permission-catalog endpoint

**Status:** Proposed — explicit approval is required before implementation because this adds a public administrative API and authorization-protected behavior.

## Outcome

Expose the server-owned, assignable human-role permission catalog to Superadmin frontends without moving the catalog to a database or changing how permissions authorize Country actions. The frontend receives stable code and English display metadata to render role-permission selection; the server remains the authority for all permission validation and authorization.

## Owning module and analogous implementation

**Confirmed:** Human role permission assignment is owned by `IdentityRoleManagementFeatures`. `AdminIdentityRolesController` dispatches through the custom `IMediator` to `IdentityRoleManagementHandler`; role persistence remains in `IIdentityRoleManagementService` and `IdentityRoleManagementService`. `HumanPermissionCatalog` in Domain owns the current allowed codes. Existing role reads use bearer-only `IdentityRoleReadPolicy`, `AuthenticatedSecurityPolicy`, `ResponseDto<T>`, and `Cache-Control: no-store`.

**Analogous implementation:** `GET /api/admin/identity/roles/{roleId}/permissions` returns a wrapped read model through the custom mediator. The proposed catalog read follows that controller-to-mediator-to-public-view-model convention, but it has no role ID and no Identity persistence operation.

## Observed flow and proposed flow

**Observed:** `HumanPermissionCatalog.All` contains only `country.read` and `country.write`. Permission replacement rejects values not present in that set. JWT issuance and Country policies use the same codes; API-client permissions use the separate `api_permission` claim type and are not allow-listed by this catalog.

**Proposed:** `GET /api/admin/identity/permissions` -> dedicated administrative API controller -> custom mediator query/handler -> `HumanPermissionCatalog` -> public catalog view model -> `ResponseDto<T>`. The handler maps static definitions only; it does not call `IIdentityRoleManagementService`, access `IdentityContext`, or write audit records.

## Files to add / modify / intentionally leave unchanged

- Modify `HumanPermissionCatalog` to expose an ordered definition collection containing each existing assignable code and its server-owned English metadata: `code`, `label`, `description`, and `category`. Preserve the existing constants and `All` lookup for all current authorization and validation callers.
- Extend `IdentityRoleManagementFeatures` with a read query and handler plus public catalog view models under the existing Identity-role view-model namespace.
- Add a dedicated controller at the `api/admin/identity/permissions` route, using the existing mediator, read policy, rate limiter, response wrapper, no-store convention, and Swagger metadata.
- Update `docs/architecture/authentication-authorization.md` and `docs/guides/test-human-user-role-permissions.md` after implementation and verification.
- Intentionally leave `IIdentityRoleManagementService`, `IdentityRoleManagementService`, `IdentityContext`, role claims, JWT issuance, Country policies, API-client services, migrations, configuration, and dependencies unchanged.

## Business behavior and confidence

**User-selected decisions:** The initial catalog is human roles only; it is code-owned rather than database-configurable; the endpoint is Superadmin-only; metadata is English only; and the endpoint returns code plus label, description, and category.

**Planned behavior:** Return all and only currently assignable human permissions in ascending ordinal code order. The initial entries are:

| Code | Label | Description | Category |
| --- | --- | --- | --- |
| `country.read` | `View countries` | `View active country reference data.` | `Country` |
| `country.write` | `Manage countries` | `Create, update, and delete country reference data.` | `Country` |

Catalog metadata is informational and must not be used by the frontend as authorization evidence. Permission replacement continues to validate its submitted codes on the server.

## Authorization and security

Apply the existing bearer-only `IdentityRoleReadPolicy` and `AuthenticatedSecurityPolicy`. A bearer Superadmin receives `200`; missing/invalid bearer retains `401`; a non-Superadmin bearer and API-key principal retain `403`; rate limit rejection is `429`. No step-up proof is required because this is read-only. Set `Cache-Control: no-store` for successful and controller-mapped failure responses, matching adjacent role reads. Do not return claim types, role-claim IDs, concurrency stamps, JWT claims, API keys, or any sensitive authentication material.

No permission-catalog read audit is introduced. This avoids adding an audit stream for static reference metadata; role permission writes retain their existing audit behavior.

## API contract

| Route | Success | Failures |
| --- | --- | --- |
| `GET /api/admin/identity/permissions` | `200 OK`, `Cache-Control: no-store`, existing `ResponseDto` wrapper around `{ "permissions": [{ "code", "label", "description", "category" }] }` | `401`, `403`, or `429` according to the existing policy and rate limiter |

No request body, query parameters, ETag, pagination, or step-up-proof header is accepted. Do not include API-client permissions in this response. The response is not localized in this release.

## Entity, persistence, and migration impact

None. The catalog remains a static Domain definition and role grants continue to use the existing `IdentityRoleClaims` storage. No entity, schema, migration, database query, runtime setting, package, or external integration is added.

## Service decision and evidence

The catalog read does not belong in the Identity service because there is no Identity persistence, `RoleManager`, `UserManager`, proof validation, or transaction work. The custom mediator handler is retained to match the existing public role-management request path and to prevent controller business mapping from becoming a new convention.

## Transaction, audit, and integration impact

There is no mutation, transaction, audit, notification, session invalidation, token change, or external integration. The endpoint must not change the existing policy interpretation: `country.read` and `country.write` still authorize only their corresponding human Country permissions, while API keys continue to use `api_permission`.

## Test plan

- Add handler tests verifying the full catalog, exact code-to-metadata mapping, and deterministic ordinal ordering.
- Add HTTP/controller authorization coverage for Superadmin bearer success; anonymous, non-Superadmin bearer, and API-key denial; rate-limiter behavior/metadata; and `Cache-Control: no-store`.
- Verify the public contract uses the existing response wrapper and contains no internal claim type, role-claim identifier, persistence detail, or sensitive material.
- Verify every returned code is accepted by the existing role-permission replacement validation, and that no API-client-only permission is exposed.
- Run the focused tests first, then:

```powershell
dotnet restore .\AhisApiTemplate.sln
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

Do not apply a migration or mutate a user database as verification.

## Documentation impact

Document the endpoint as the frontend discovery source for assignable human role permissions in the authentication/authorization architecture guide and human-permission test guide. Swagger/XML documentation must state the Superadmin bearer requirement, no-store response behavior, response fields, and actual status outcomes. Keep API-client permissions documented as a separate model.

## Risks, assumptions, and open questions

**Risk:** A frontend could incorrectly treat catalog presence as current-user authority. Mitigation: document that it is for administrative role editing only; the API continues to enforce policies and validates every assignment.

**Assumptions:** Labels and descriptions are stable English product copy for this release. `HumanPermissionCatalog` remains the sole authoritative source for assignable human permission codes. Additional permissions require a reviewed code change and corresponding authorization design; they are not dynamically inserted into a database.

**Open questions:** None for the defined first release. Localization, runtime-configurable permissions, per-tenant catalogs, direct user grants, unifying API-client permissions, or applying new permissions to other endpoints require separate reviewed changes.
