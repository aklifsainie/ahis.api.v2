# API Client Authentication Business Rules

## KEY-001 — One-time raw-key disclosure

The raw API key is returned only when a client/key is created. Only SHA-256 hash and non-secret prefix are stored. Confidence: Confirmed.

## KEY-002 — Client identifier normalization

Client identifiers are trimmed, lowercased, and spaces replaced with hyphens. They are unique. Confidence: Confirmed.

## KEY-003 — Permission normalization

Blank permissions are removed; values are trimmed, lowercased, and deduplicated case-insensitively. The database enforces uniqueness per client. Confidence: Confirmed.

## KEY-004 — Future expiry

An explicitly supplied key expiry must be later than current UTC. Confidence: Confirmed.

## KEY-005 — Authentication eligibility

Authentication requires a known active key that is not revoked or expired and an active owning client. Confidence: Confirmed.

## KEY-006 — Generic key failures

The external authentication response does not disclose whether a key is unknown, revoked, expired, or belongs to an inactive client. Confidence: Confirmed.

## KEY-007 — Client deactivation

Client deactivation records its reason/time/actor and revokes every active key. Confidence: Confirmed.

## KEY-008 — Key revocation idempotency

Revoking an already inactive or revoked key returns without changing it again. Confidence: Confirmed.
