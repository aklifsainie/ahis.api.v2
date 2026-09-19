# Country Data Model

`Country` uses an integer SQL identity key inherited from `BaseEntity` and stores:

- Full and short names
- Optional description
- Two-letter and three-letter alphabetic codes
- Three-digit numeric ISO code stored as a string
- Active, soft-delete, registration/update, and remarks fields inherited from `BaseEntity`

EF maps to table `Country`, limits string lengths, uses non-Unicode columns for codes, and creates unique indexes on full name and all three codes. `CountryVM` exposes the ID, names/codes/description, and active state, although not every handler currently sets `IsActive` explicitly.
