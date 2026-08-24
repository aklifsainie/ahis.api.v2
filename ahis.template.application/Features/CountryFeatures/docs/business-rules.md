# Country Business Rules

## CTY-001 — Active read visibility

List and GetById return only active, non-deleted countries. Non-deleted filtering is enforced by generic repository queries; handlers add `IsActive`. Tests cover list filtering. Confidence: Confirmed.

## CTY-002 — Unique identifiers

`CountryFullname`, `CountryCode2`, `CountryCode3`, and `CountryIsoCode` must be unique. Handlers check duplicates and EF defines unique indexes. Confidence: Confirmed.

## CTY-003 — Country-code formats

Alpha-2 is two letters, alpha-3 is three letters, and numeric ISO code is three digits. Alpha codes are persisted uppercase; inputs are trimmed. Data Annotations and handler validation enforce this, although the FluentValidation alpha-3 rule currently permits two or three letters before handler validation runs. Confidence: Confirmed, with implementation inconsistency.

## CTY-004 — Soft deletion

Delete sets `IsDelete=true` and `IsActive=false`; it does not remove the row. Enforcement: `DeleteCountryCommandHandler`. Confidence: Confirmed.

## CTY-005 — Read auditing

GetAll and GetById explicitly record `AuditActionEnum.View`. Enforcement: query handlers. Confidence: Confirmed.
