# Authentication Use Cases

- Check email/username account state without confirming whether an unknown account exists.
- Authenticate with username/email and password.
- Complete login with authenticator or recovery code when 2FA is required.
- Rotate an unexpired refresh token and issue a new access token.
- Revoke the current refresh token during logout.
- Send a password-reset email and reset a password with an Identity token.
- Decode or encode JWTs through diagnostic endpoints.

The encode/decode endpoints currently have no explicit authorization and should not be treated as a recommended production pattern.
