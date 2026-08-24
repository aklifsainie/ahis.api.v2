# Authentication Status Transitions

```text
Password login
  -> Failed / LockedOut
  -> RequiresTwoFactor (no tokens)
  -> Authenticated (access + refresh token)

Active refresh token
  -> Rotated: old revoked, replacement active
  -> Expired: rejected
  -> Revoked token reused: all active user refresh tokens revoked

Active session
  -> Logout: supplied usable refresh token revoked
  -> Password change/reset: Identity security stamp updated
```

Refresh-token revocation is represented by `IsRevoked` and usually `RevokedAt`; bulk revocation currently sets only `IsRevoked`.
