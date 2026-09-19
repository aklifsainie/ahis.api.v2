# Country Use Cases

- **List active countries**: Return active/non-deleted records sorted case-insensitively by full name; an empty list is a successful result.
- **Get active country by ID**: Validate positive ID; return not found when inactive, deleted, or absent.
- **Create country**: Normalize and validate values, reject duplicate unique fields, insert, save, and return HTTP 201 through the controller.
- **Update country**: Bind route ID into the command, load a tracked row, validate uniqueness excluding itself, update fields/status, and save.
- **Delete country**: Validate positive ID, load tracked row, set deletion/deactivation flags, save, and return HTTP 204.
