# Test human permission catalog

Use a non-production environment and an active, confirmed account currently assigned the `Superadmin` role. Keep bearer tokens, API keys, user IDs, and other credentials out of command history, logs, tickets, and source control.

## Prerequisites

- The API is available at `https://<api-host>`.
- Sign in as a Superadmin and retain `data.accessToken` as `<superadmin-access-token>`.
- The role-management policies and authenticated-security rate limiter are enabled.

## Retrieve the catalog

```http
GET https://<api-host>/api/admin/identity/permissions
Authorization: Bearer <superadmin-access-token>
```

Expect `200 OK`, `Cache-Control: no-store`, and the normal response wrapper. `data.permissions` must be ordered by code and contain exactly:

```json
[
  {
    "code": "country.read",
    "label": "View countries",
    "description": "View active country reference data.",
    "category": "Country"
  },
  {
    "code": "country.write",
    "label": "Manage countries",
    "description": "Create, update, and delete country reference data.",
    "category": "Country"
  }
]
```

The response must not expose claim types, role-claim IDs, concurrency stamps, JWT claims, API keys, or other credentials.

## Verify authorization

| Caller | Expected result |
| --- | --- |
| No bearer credential | `401 Unauthorized` |
| Authenticated user without `Superadmin` | `403 Forbidden` |
| API key, with no bearer credential | `403 Forbidden` |
| Bearer Superadmin | `200 OK` with the catalog |

The endpoint is read-only and does not require an `X-Step-Up-Proof` header. It must not create audit records, change role claims, invalidate sessions, or issue new tokens.

## Verify role-editor use

Use the returned `code` values, not the display labels, in the existing complete-set role-permission request. For example, create a disposable custom role, retrieve its ETag, and submit `country.read` to `PUT /api/admin/identity/roles/<role-id>/permissions` as documented in [the human role-permissions guide](test-human-user-role-permissions.md). Confirm the server accepts returned codes and continues to reject unknown codes.

## Rate-limit check

The endpoint uses `AuthenticatedSecurityPolicy`, currently five requests per minute per authenticated user. Send requests only in a disposable environment; the next request after the permitted count should return `429 Too Many Requests`. Wait for the fixed window to reset before further checks.

## Automated verification

From the repository root:

```powershell
dotnet test .\ahis.template.test\ahis.template.test.csproj --no-restore --filter "FullyQualifiedName~IdentityRoleManagementHandler"
dotnet restore .\AhisApiTemplate.sln
dotnet build .\AhisApiTemplate.sln --no-restore
dotnet test .\AhisApiTemplate.sln --no-build --no-restore
```

Do not apply a migration or use a production database for verification.
