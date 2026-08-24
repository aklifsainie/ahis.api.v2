# API Client and Key Status Transitions

```text
Client created -> Active
Active client -> Deactivated
  -> all active keys revoked

Key created -> Active
Active key -> Revoked
Active key -> Expired (time-based eligibility; IsActive may remain true)
```

No reactivation workflow is implemented. Re-revocation is idempotent. Expiration is evaluated at authentication time rather than by a background processor.
