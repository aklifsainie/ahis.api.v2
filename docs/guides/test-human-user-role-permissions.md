# Test P2 human-user role permissions

Run this against a non-production environment with a disposable custom role and a disposable eligible target account. Do not use the first or only Superadmin as the target. Keep passwords, bearer tokens, step-up proofs, and user IDs out of command history, logs, tickets, and source control.

## Prerequisites

- The roles-only P2 setup has completed, including a current active Superadmin and a second active, confirmed target user.
- The API is available at `https://<api-host>`.
- Sign in as the Superadmin and retain `data.accessToken` as `<superadmin-access-token>`.
- Obtain a current five-minute step-up proof from `POST /api/account/re-authenticate` and retain it as `<step-up-proof>`.

## 1. Create and inspect a custom role

Create a disposable role using the existing role endpoint, then save its returned ID and ETag.

```http
POST https://<api-host>/api/admin/identity/roles
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
Content-Type: application/json

{ "name": "PermissionTestReader" }
```

Expect `201 Created`. Save `data.id` as `<role-id>` and the ETag as `<role-etag>`. Inspect its permissions:

```http
GET https://<api-host>/api/admin/identity/roles/<role-id>/permissions
Authorization: Bearer <superadmin-access-token>
```

Expect `200 OK`, `Cache-Control: no-store`, an ETag, and `data.permissions: []`.

## 2. Assign Country read permission

```http
PUT https://<api-host>/api/admin/identity/roles/<role-id>/permissions
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
If-Match: <role-etag>
Content-Type: application/json

{ "permissions": ["country.read"] }
```

Expect `204 No Content`, `Cache-Control: no-store`, and a replacement ETag. Repeat the GET and confirm `data.permissions` is exactly `["country.read"]`. The existing role-detail GET must show the same permissions collection.

Repeat the same PUT with the replacement ETag. Expect `204`; this is a no-op. Use the prior ETag after a changed PUT and expect `412 Precondition Failed`; omit it and expect `428 Precondition Required`.

## 3. Assign the role and verify bearer access

Use the existing complete-set user-role endpoint to add `<role-id>` to the target's current safe direct roles. The changed assignment invalidates the target’s existing sessions. Sign in again as the target and save its new access token as `<target-access-token>`.

```http
GET https://<api-host>/api/v1/country
Authorization: Bearer <target-access-token>
```

Expect `200 OK`. Attempt a Country write with the same token and expect `403 Forbidden`, because `country.read` does not grant writes.

## 4. Replace with Country write permission

Use the current role ETag to replace the desired set:

```http
PUT https://<api-host>/api/admin/identity/roles/<role-id>/permissions
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <fresh-step-up-proof>
If-Match: <current-role-etag>
Content-Type: application/json

{ "permissions": ["country.write"] }
```

Expect `204`. The target’s old access token and refresh session must now fail. Sign in again as the target. A Country write should be authorized; Country read should be `403` unless the target also has `User`, `Superadmin`, or another role granting `country.read`.

## 5. Validate protections and input

| Test | Expected result |
| --- | --- |
| `permissions: ["country.read", "country.read"]` | `400 Bad Request` |
| `permissions: ["unknown.permission"]` | `400 Bad Request` |
| Missing body or `permissions: null` | `400 Bad Request` |
| PUT to `Superadmin` or `User` role | `409 Conflict` |
| Unknown role ID | `404 Not Found` |
| API key calling either permissions endpoint | `403 Forbidden` |

## 6. Verify audit and cleanup

For each changed set, query the target role’s audit history and confirm a `RolePermissionsReplaced` record with sorted `addedPermissions` and `removedPermissions`. It must not contain proof, bearer/refresh tokens, security stamps, credentials, user profiles, arbitrary claims, or claim IDs. A no-op PUT creates no mutation audit.

Restore the target's original safe role set, obtain a fresh proof if needed, clear the custom role’s permissions using `{ "permissions": [] }`, then delete the disposable role. Confirm `204` for each successful cleanup operation.

## Automated verification

From the repository root:

```powershell
dotnet test .\ahis.template.test\ahis.template.test.csproj --no-restore --filter "FullyQualifiedName~IdentityRoleManagement"
dotnet restore .\AhisApiTemplate.sln
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

The current focused suite uses disposable SQL Server LocalDB databases and covers role-permission replacement, no-op behavior, validation, protected roles, and assigned-user invalidation. HTTP policy and authorization coverage remain pending.
