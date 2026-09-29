# Test API-client authentication

This runbook tests the API-client authentication and authorization behavior implemented on 2026-09-29. Run it only against a development or isolated test environment because it creates clients, keys, audit records, and, optionally, Country data.

The examples use `https://<api-host>` and placeholders. Keep passwords, bearer tokens, step-up proofs, and raw API keys out of terminals with shared history, logs, tickets, and source control.

## Before starting

You need an active, confirmed user with the protected `Superadmin` role. The API must be configured with its normal JWT and database settings, and the Superadmin must be able to complete password login. The [role assignment guide](register-and-assign-user-role.md) describes how to prepare an ordinary `User`; use the existing bootstrap flow for the first Superadmin.

For an API client that will test both Country reads and writes, create it with both `country.read` and `country.write`. These permissions are independent: `country.write` does not grant reads.

Use a REST client environment or local shell variables for these values:

```text
API_BASE=https://<api-host>
SUPERADMIN_ACCESS_TOKEN=<superadmin-access-token>
STEP_UP_PROOF=<five-minute-step-up-proof>
API_CLIENT_ID=<api-client-guid>
API_KEY_ID=<api-key-guid>
RAW_API_KEY=<raw-key-returned-once>
```

## 1. Sign in as Superadmin

Call `POST /api/authentication/login` with the Superadmin's username or email and password.

```http
POST https://<api-host>/api/authentication/login
Content-Type: application/json

{
  "userNameOrEmail": "superadmin@example.test",
  "password": "<superadmin-password>",
  "rememberMe": false
}
```

On a completed login, copy the returned `accessToken` into `SUPERADMIN_ACCESS_TOKEN`. If the response requires two-factor authentication, complete `POST /api/authentication/verify-2fa` first; do not use an incomplete-login response as an authenticated session.

## 2. Obtain a step-up proof

API-client administration requires a current five-minute proof tied to the Superadmin's account and security version.

```http
POST https://<api-host>/api/account/re-authenticate
Authorization: Bearer <superadmin-access-token>
Content-Type: application/json

{
  "password": "<superadmin-password>",
  "twoFactorCode": "<optional-authenticator-code>"
}
```

Success is `200 OK` with a `stepUpProof` field. Copy it into `STEP_UP_PROOF` and use it promptly. Obtain a new proof if five minutes pass or if the Superadmin's security state changes.

## 3. Verify that administration rejects anonymous and API-key callers

Before creating anything, call the administration endpoint with no credentials:

```http
POST https://<api-host>/api/apiclient
Content-Type: application/json

{}
```

Expected result: `401 Unauthorized`.

After step 4 creates a key, repeat the same request using only `X-API-Key: <raw-api-key>`. It must also return `401 Unauthorized`; API keys can access permitted resources but cannot administer API clients.

## 4. Create a test API client and initial key

Use the Superadmin bearer token and step-up proof. A low rate limit makes the rate-limit test quick; use `2` for this runbook.

```http
POST https://<api-host>/api/apiclient
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
Content-Type: application/json

{
  "clientId": "api-auth-test-client",
  "clientName": "API authentication test client",
  "description": "Development-only authentication test client",
  "contactName": "Test Operator",
  "contactEmail": "test.operator@example.test",
  "rateLimitPerMinute": 2,
  "initialKeyName": "Initial test key",
  "permissions": [
    "country.read",
    "country.write"
  ]
}
```

`keyExpiresAt` is optional. Leave it out as shown for a non-expiring test key, or add a real future UTC value such as `"keyExpiresAt": "2027-12-31T23:59:59Z"`. Do not send the angle-bracket placeholder as JSON; it is documentation text, not a DateTime.

Expected result: `201 Created` and `Cache-Control: no-store`. Save these response fields immediately in the local secure test environment:

- `id` as `API_CLIENT_ID`;
- `apiKey` as `RAW_API_KEY`; and
- `keyPrefix` only as a non-secret identifier for troubleshooting.

The raw `apiKey` is returned once and cannot be recovered later. Do not add it to the guide, test fixtures, or audit metadata.

Repeat the request without `X-Step-Up-Proof`; expected result is `400 Bad Request`. Repeat it with a valid bearer token for a non-Superadmin; expected result is `403 Forbidden`.

## 5. Test Country read and write permissions

Use only the API-key header for this section.

```http
GET https://<api-host>/api/v1/country
X-API-Key: <raw-api-key>
```

Expected result: `200 OK` when the client has `country.read`.

Create a Country record to verify `country.write`:

```http
POST https://<api-host>/api/v1/country
X-API-Key: <raw-api-key>
Content-Type: application/json

{
  "countryFullname": "API Authentication Testland",
  "countryShortname": "Auth Testland",
  "countryDescription": "Temporary development test record",
  "countryCode2": "AT",
  "countryCode3": "AUT",
  "countryIsoCode": "998"
}
```

Expected result: `201 Created`. Record the returned Country ID and delete this temporary record through the same API key after the test:

```http
DELETE https://<api-host>/api/v1/country/<temporary-country-id>
X-API-Key: <raw-api-key>
```

Expected result: `204 No Content`.

To prove permission denial, create another client with only `country.read`, then try the POST or DELETE Country actions using that client's key. Expected result: `403 Forbidden`. A client with no Country permission must receive `403` for both Country reads and writes.

For bearer access, a bearer `User` can call Country GET actions but receives `403` for Country writes. A bearer `Superadmin` can call both. If both `Authorization` and `X-API-Key` headers are sent, Bearer takes precedence. An invalid bearer plus a valid API key must return `401` rather than falling back to the key.

## 6. Test the per-client rate limit

The client from step 4 has a two-request-per-minute Country quota shared by all of its keys. Start after waiting for a fresh minute-long window, then send three Country GET requests using `RAW_API_KEY`.

```http
GET https://<api-host>/api/v1/country
X-API-Key: <raw-api-key>
```

The first two requests should succeed if no earlier Country calls used the same key/client in the current window. The third should return `429 Too Many Requests` with the API's JSON error response. Wait for the window to replenish before continuing.

Create a second client with a separate `clientId` and rate limit, then confirm its Country request is unaffected by the first client's exhausted quota. The quota is per application instance; it resets if the service restarts and is not shared across separate application replicas.

## 7. Test key rotation and revocation

Obtain a fresh step-up proof if necessary, then create an additional key.

```http
POST https://<api-host>/api/apiclient/<api-client-guid>/keys
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
Content-Type: application/json

{
  "keyName": "Rotation test key"
}
```

`expiresAt` is also optional. To set it, add a real future UTC value such as `"expiresAt": "2027-12-31T23:59:59Z"`.

Expected result: `201 Created`. Save the returned `keyId` and raw `apiKey` only long enough to verify that both the old and new keys can call the permitted Country endpoint.

Revoke the original key:

```http
POST https://<api-host>/api/apiclient/<api-client-guid>/keys/<original-key-guid>/revoke
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
Content-Type: application/json

{
  "reason": "Development authentication test complete"
}
```

Expected result: `204 No Content`. The revoked key must then receive `401 Unauthorized` from Country endpoints, while the rotation key continues to work. Repeating the same revoke remains `204 No Content` and does not create another mutation audit event.

## 8. Test client deactivation

Deactivate the test client after validating rotation:

```http
POST https://<api-host>/api/apiclient/<api-client-guid>/deactivate
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
Content-Type: application/json

{
  "reason": "Development authentication test cleanup"
}
```

Expected result: `204 No Content`. Every key for the client, including its rotation key, must then receive `401 Unauthorized` from Country endpoints. Deactivation is permanent through the current API; use a unique development client for each full lifecycle test.

## 9. Verify audit access and records

API keys and ordinary bearer users cannot read audit history. Both requests below must fail: API-key-only calls return `401`; a bearer User call returns `403`.

```http
GET https://<api-host>/api/auditlog?entityName=ApiClient&pageNumber=1&pageSize=20
X-API-Key: <raw-api-key>
```

Use the Superadmin bearer token to read the records:

```http
GET https://<api-host>/api/auditlog?entityName=ApiClient&pageNumber=1&pageSize=20
Authorization: Bearer <superadmin-access-token>
```

Expected result: `200 OK` with `Cache-Control: no-store`. Confirm the lifecycle actions produce records for `ApiClient` and `ApiClientKey`. Audit metadata identifies the operation and target IDs but must not expose raw keys, hashes, step-up proofs, tokens, contact details, or free-text revocation reasons.

## Expected failure responses

| Situation | Expected response |
| --- | --- |
| Missing or invalid selected credential | `401 Unauthorized` |
| Bearer user or API key lacks required resource access | `403 Forbidden` |
| Superadmin sends no, expired, mismatched, or duplicate step-up proof | `400 Bad Request` |
| Country quota is exhausted | `429 Too Many Requests` |
| API client/key does not exist during administration | `404 Not Found` |
| Duplicate client ID or invalid active-client key creation | `409 Conflict` |

## Cleanup and operational checks

Use client deactivation as the cleanup action because it revokes all active keys. Do not attempt to reuse a deactivated client. Remove the temporary Country record before deactivation if it was created.

After the test, confirm server logs contain no raw API key, bearer token, step-up proof, password, or request header value. The handler may record a key GUID and failure category for best-effort `LastUsedAt` tracking; that is expected and is not secret material.
