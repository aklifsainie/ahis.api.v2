# API Client Authentication Data Model

## ApiClient

GUID key, unique client identifier, display/contact fields, active state, configured rate-per-minute, creation/update metadata, and deactivation metadata. Owns keys and permissions.

## ApiClientKey

GUID key, client foreign key, name, non-secret prefix, unique 64-character SHA-256 hash, active state, creation/expiry/last-used/revocation metadata. Delete behavior from client is restricted.

## ApiClientPermission

GUID key, client foreign key, permission code, and creation metadata. `(ApiClientId, PermissionCode)` is unique; client deletion cascades to permissions.

All GUIDs are generated in application code and configured `ValueGeneratedNever`.
