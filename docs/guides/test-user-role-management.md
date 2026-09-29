# Test P2 user role management

This runbook tests the implemented roles-only v1 role-management flow. Run it against a non-production environment with a disposable target account. Do not use the first or only Superadmin account as the target.

## Prerequisites

- The Identity role-store and bootstrap migrations are applied through the normal deployment process.
- The API is available at `https://<api-host>`.
- A current, active, confirmed Superadmin account can sign in.
- A second active, confirmed, eligible account exists as the target user.
- Keep the following values private and out of command history, logs, tickets, and source control:

  - `<superadmin-password>`
  - `<superadmin-access-token>`
  - `<step-up-proof>`
  - `<target-user-id>`

The role-write proof lasts five minutes. Obtain a fresh proof before role creation, renaming, deletion, or role assignment if needed.

## 1. Sign in as Superadmin

```http
POST https://<api-host>/api/authentication/login
Content-Type: application/json

{
  "userNameOrEmail": "<superadmin-username-or-email>",
  "password": "<superadmin-password>",
  "rememberMe": false
}
```

Expect `200 OK`. Copy `data.accessToken` as `<superadmin-access-token>`. If `data.requiresTwoFactor` is `true`, complete the normal two-factor verification flow before continuing.

## 2. Obtain a step-up proof

```http
POST https://<api-host>/api/account/re-authenticate
Authorization: Bearer <superadmin-access-token>
Content-Type: application/json

{
  "password": "<superadmin-password>",
  "twoFactorCode": "<authenticator-code-if-required>"
}
```

Expect `200 OK` with `stepUpProof`. Save it as `<step-up-proof>` and send it only in the `X-Step-Up-Proof` header for write requests.

## 3. List and inspect protected roles

```http
GET https://<api-host>/api/admin/identity/roles?pageNumber=1&pageSize=20
Authorization: Bearer <superadmin-access-token>
```

Expect `200 OK`, `Cache-Control: no-store`, and paged role data. Confirm `Superadmin` and `User` have `isProtected: true`. Record the role IDs as `<superadmin-role-id>` and `<user-role-id>`.

Inspect one role:

```http
GET https://<api-host>/api/admin/identity/roles/<user-role-id>
Authorization: Bearer <superadmin-access-token>
```

Expect `200 OK`, an opaque `ETag` header, `isProtected: true`, and an empty `permissions` collection.

## 4. Create and verify a custom role

```http
POST https://<api-host>/api/admin/identity/roles
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
Content-Type: application/json

{
  "name": "RoleTestOperator"
}
```

Expect `201 Created`, `Location`, `ETag`, and `Cache-Control: no-store`. Save `data.id` as `<test-role-id>` and the returned ETag as `<test-role-etag>`.

Repeat the create request with the same name. Expect `409 Conflict`. Retry with a blank name, an invalid character, or `Superadmin`; expect `400` or `409` as appropriate.

## 5. Rename the custom role with ETag protection

```http
PUT https://<api-host>/api/admin/identity/roles/<test-role-id>
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
If-Match: <test-role-etag>
Content-Type: application/json

{
  "name": "RoleTestOperatorRenamed"
}
```

Expect `200 OK` and a replacement `ETag`. Repeat with the old ETag and expect `412 Precondition Failed`. Omit `If-Match` and expect `428 Precondition Required`.

Attempt to rename `<user-role-id>` or `<superadmin-role-id>` with a current proof and ETag. Expect `409 Conflict`.

## 6. Read the target user's direct roles

```http
GET https://<api-host>/api/admin/identity/users/<target-user-id>/roles
Authorization: Bearer <superadmin-access-token>
```

Expect `200 OK`, `Cache-Control: no-store`, and only direct role assignments. Preserve every returned ID; the replacement endpoint replaces the complete set.

## 7. Replace the target user's complete role set

Build the desired set from the target's existing IDs plus `<test-role-id>` (or use only test-safe roles for a dedicated target).

```http
PUT https://<api-host>/api/admin/identity/users/<target-user-id>/roles
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
Content-Type: application/json

{
  "roleIds": [
    "<test-role-id>"
  ]
}
```

Expect `204 No Content` and `Cache-Control: no-store`. A changed assignment invalidates the target's access and refresh sessions, so sign in again as the target before testing protected resources.

Repeat the GET from step 6 and confirm that it lists `RoleTestOperatorRenamed`. Repeat the same PUT unchanged; expect another `204` with no membership change.

## 8. Verify assignment protections

Run these checks with the same authenticated Superadmin:

| Test | Request change | Expected result |
| --- | --- | --- |
| Missing proof | Omit `X-Step-Up-Proof` from a PUT | `400 Bad Request` |
| Unknown role | Submit a nonexistent role ID | `400 Bad Request` and no partial change |
| Duplicate role ID | Submit the same ID twice | `400 Bad Request` |
| Self assignment | Use the Superadmin's own user ID as `<target-user-id>` | `400 Bad Request` |
| Protected role deletion | Delete `<user-role-id>` or `<superadmin-role-id>` | `409 Conflict` |
| Assigned role deletion | Delete `<test-role-id>` while assigned | `409 Conflict` |
| Missing target | Use an unknown user ID in GET or PUT | `404 Not Found` |

Use a separate approved test setup when testing the last-effective-Superadmin rule. Never attempt it against the only production administrator.

## 9. Verify users assigned to a role

```http
GET https://<api-host>/api/admin/identity/roles/<test-role-id>/users?pageNumber=1&pageSize=20
Authorization: Bearer <superadmin-access-token>
```

Expect `200 OK`, bounded pagination, and user ID/username projections only. Confirm `<target-user-id>` is present. An unknown role ID returns `404 Not Found`.

## 10. Verify the redacted audit record

Use a Superadmin bearer token to retrieve audit history for the target:

```http
GET https://<api-host>/api/auditlog/entity/IdentityRoleManagement/<target-user-id>?pageNumber=1&pageSize=20
Authorization: Bearer <superadmin-access-token>
```

Find the record created by the changed assignment. Confirm its metadata is JSON with this shape:

```json
{
  "action": "UserRolesReplaced",
  "addedRoleIds": ["<test-role-id>"],
  "removedRoleIds": ["<previous-role-id>"]
}
```

Confirm the arrays are sorted ordinally and contain role IDs only. The record must not contain role names, passwords, step-up proofs, bearer or refresh tokens, security stamps, or user-profile data. Repeating an unchanged PUT must not create a second mutation audit row.

## 11. Remove the test assignment and role

Obtain a fresh step-up proof if the previous one has expired. Replace the target's role set with its original safe role IDs, then delete the custom role:

```http
DELETE https://<api-host>/api/admin/identity/roles/<test-role-id>
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <fresh-step-up-proof>
```

Expect `204 No Content`. Repeating deletion returns `404 Not Found`.

## 12. Run automated verification

From the repository root:

```powershell
dotnet test .\ahis.template.test\ahis.template.test.csproj --no-restore --filter "FullyQualifiedName~IdentityRoleManagement"
dotnet restore .\AhisApiTemplate.sln
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

The focused tests use disposable SQL Server LocalDB databases and cover audit metadata, changed/no-op replacement, and rollback when target token invalidation fails. They do not yet cover HTTP authorization or concurrent Superadmin demotion; those gaps remain recorded in the backlog.
