# Overview

Several issues happen after implementing `P0 - Existing endpoint hardening` from the backlog file here `docs/backlog/identity-security-endpoints.md`

## 1. isEmailConfirmed Return False Even After Verify Email

### Details

I have register an account and verify the email. I also have enable the 2FA. Then, I tried to login. But the response of `isEmailConfirmed` return `false`. Should be `true`.

Endpoint: `POST /api/Authentication/login`

Response:

```json
{
  "succeeded": true,
  "message": "Successfully logged in",
  "errors": [],
  "data": {
    "accessToken": "",
    "expiresInSeconds": 0,
    "requiresTwoFactor": true,
    "isEmailConfirmed": false
  }
}
```

## 2. Unable To Login 2FA

### Details

After login an account via endpoint `POST /api/Authentication/login` that have enabled the 2FA, I got the response as below:

```json
{
  "succeeded": true,
  "message": "Successfully logged in",
  "errors": [],
  "data": {
    "accessToken": "",
    "expiresInSeconds": 0,
    "requiresTwoFactor": true,
    "isEmailConfirmed": false
  }
}
```

Then, I try to execute the verify 2FA endpoint but I cannot log into the API. As below:

Endpoint: `POST /api/Authentication/verify-2fa`

Response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "general": ["Invalid or expired two-factor challenge."]
  },
  "traceId": "00-a1af479d29e00c8ddc6abf19d9a30d39-64f0c4a07732f3a4-00"
}
```

## Request

- Kindly help to analyze the code and try to understand which part does the error coming from.
- I also have tried to debug for issue #2, it seems like the HttpContext returning isSuccess as false in `ahis.template.identity.Services/AuthenticationService.cs:299`
- Identity the error then explain to me the root cause.
- After that, come out with a blueprint on how to solve it with detailing the which files need to be added / edited / deleted / etc.
- Wait for my approval before implementing it
