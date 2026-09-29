# Register a user and assign the `User` role

This runbook describes the API flow as implemented on 2026-09-29. It assumes:

- the API is running at `https://<api-host>`;
- email delivery and `Identity:PublicClientBaseUrl` are configured; and
- at least one active, confirmed Superadmin already exists and can sign in.

Registration does **not** assign a role. It creates an active account without a password, sends an email-confirmation link, and then sends a separate password-setup link after confirmation. The Superadmin assigns the protected `User` role only after the account is able to authenticate.

## 1. Register the account

Call `POST /api/account/register` without authentication.

```http
POST https://<api-host>/api/account/register
Content-Type: application/json

{
  "email": "new.user@example.test",
  "userName": "new.user",
  "callbackBaseUrl": "https://<public-client-host>"
}
```

Success is `204 No Content`. The account has no password and no role at this point.

`callbackBaseUrl` is required by the request model. The current implementation builds the email links from the server-side `Identity:PublicClientBaseUrl` setting, so that setting must point to the trusted public client. Do not put credentials, tokens, or secrets in either value.

## 2. Confirm the email address

The confirmation email links the public client to a URL equivalent to:

```text
https://<public-client-host>/account/confirm-email?userId=<new-user-id>&token=<encoded-token>
```

Have the public client read the `userId` and `token` from that link and send them to the API; the email link itself is not an API `GET` endpoint.

```http
POST https://<api-host>/api/account/confirm-email
Content-Type: application/json

{
  "userId": "<new-user-id>",
  "token": "<encoded-confirmation-token>"
}
```

Success is `204 No Content`. Confirmation records the verified email and sends the user a new password-setup email. Retain the returned/linked user ID only as needed for the later administrative assignment.

## 3. Set the initial password

The second email links the public client to a URL equivalent to:

```text
https://<public-client-host>/set-password?userId=<new-user-id>&token=<encoded-setup-token>
```

Submit those values with the new password:

```http
POST https://<api-host>/api/account/set-password
Content-Type: application/json

{
  "userId": "<new-user-id>",
  "token": "<encoded-setup-token>",
  "password": "<new-password>"
}
```

Success is `204 No Content`. The setup token is purpose-bound, single-use in practice because this endpoint rejects accounts that already have a password, and expires after 30 minutes. The password must meet the environment's ASP.NET Identity password policy. The checked-in development policy requires at least eight characters, a digit, and an uppercase letter; confirm the deployed configuration before presenting its exact rules to users.

## 4. Confirm that the new account can sign in

Call `POST /api/authentication/login` with the new username or email and password.

```http
POST https://<api-host>/api/authentication/login
Content-Type: application/json

{
  "userNameOrEmail": "new.user@example.test",
  "password": "<new-password>",
  "rememberMe": false
}
```

On a normal successful login, retain the returned `accessToken` for authenticated calls. The refresh token is set only as an `HttpOnly`, `Secure` cookie and is not returned in the response. If `requiresTwoFactor` is `true`, finish the `POST /api/authentication/verify-2fa` flow before continuing.

## 5. Sign in as a Superadmin and obtain a step-up proof

The role APIs require a current Superadmin bearer token and a five-minute proof of the Superadmin's password (and, when applicable, two-factor code). Sign in as that Superadmin via the same login endpoint, then call:

```http
POST https://<api-host>/api/account/re-authenticate
Authorization: Bearer <superadmin-access-token>
Content-Type: application/json

{
  "password": "<superadmin-password>",
  "twoFactorCode": "<optional-authenticator-code>"
}
```

The response contains `stepUpProof`. Treat it as sensitive, keep it out of logs, and use it promptly in the `X-Step-Up-Proof` header.

## 6. Find the protected `User` role ID

Using the same Superadmin bearer token, request the role catalog:

```http
GET https://<api-host>/api/admin/identity/roles?name=User&pageNumber=1&pageSize=20
Authorization: Bearer <superadmin-access-token>
```

Read the ID of the result whose name is exactly `User` (normally `data[0].id`). The application creates the protected `Superadmin` and `User` roles at startup; do not attempt to create, rename, or delete `User`.

## 7. Preserve any existing roles, then replace the user's role set

The assignment endpoint replaces the **complete** direct-role set. For a just-registered account it should be empty, but read it first so the procedure is safe if the assignment is retried or delayed:

```http
GET https://<api-host>/api/admin/identity/users/<new-user-id>/roles
Authorization: Bearer <superadmin-access-token>
```

Build `roleIds` from the returned role IDs plus the `User` role ID, with no duplicates. For a newly registered user, the body will normally contain only the `User` ID:

```http
PUT https://<api-host>/api/admin/identity/users/<new-user-id>/roles
Authorization: Bearer <superadmin-access-token>
X-Step-Up-Proof: <step-up-proof>
Content-Type: application/json

{
  "roleIds": [
    "<user-role-id>"
  ]
}
```

Success is `204 No Content`. A Superadmin cannot change their own roles. The target must be active, not deleted, and otherwise eligible to authenticate. A successful changed assignment invalidates the target's current access and refresh sessions, so ask the new user to sign in again after assignment.

## 8. Verify the assignment

Repeat the read request from step 7 and confirm that `data` includes an item with `name: "User"` and the expected role ID.

```http
GET https://<api-host>/api/admin/identity/users/<new-user-id>/roles
Authorization: Bearer <superadmin-access-token>
```

## Operational notes

- Registration, confirmation, password setup, login, and role changes are rate limited. A `429` means wait and retry rather than rapidly repeating the request.
- `400` from confirmation or password setup generally indicates missing, invalid, expired, or already-used link data. Start from registration or use the available confirmation-resend flow as appropriate.
- `401` or `403` on administrative routes means the Superadmin bearer token is absent, invalid, expired, or lacks the current `Superadmin` membership. A missing or expired step-up proof causes the role write to fail validation.
- Never place the password, confirmation token, password-setup token, refresh token, access token, or step-up proof in tickets, source control, or logs.
