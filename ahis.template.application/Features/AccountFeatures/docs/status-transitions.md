# Account Status Transitions

```text
Registered
  -> EmailConfirmed
  -> PasswordCreated
  -> AccountConfigured

2FA Disabled
  -> Setup Generated (key exists, 2FA still disabled)
  -> 2FA Enabled (valid code, recovery codes generated)
  -> 2FA Disabled (authenticator material cleared)
```

These flags are independent in storage; the code does not implement a single account-status enum. Active/deleted and lockout states additionally govern authentication.
