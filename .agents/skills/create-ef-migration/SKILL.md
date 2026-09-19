---
name: create-ef-migration
description: Inspect and, only after explicit approval, generate an EF Core migration for the correct ApplicationDbContext or IdentityContext without applying it to a database.
---

# Create EF Migration

Never generate or apply a migration immediately. Read root/module instructions, `docs/architecture/persistence.md`, entity/configuration changes, the correct context, current snapshot, recent migrations, and project package versions.

Produce a **Migration Blueprint** containing:

- Context, migration project, and startup project
- Entities and tables affected
- Columns, types, nullability, defaults, indexes, unique constraints, foreign keys, and delete behavior
- Rename versus drop/create behavior
- Potentially destructive changes and existing-data/backfill risks
- Known model/migration inconsistencies relevant to the context
- Recommended migration name and exact command
- Verification plan

Wait for explicit approval. After approval, generate the migration, inspect every operation and snapshot change, build the affected projects, and report it. Never run `dotnet ef database update` unless the user separately and explicitly requests applying it to a named target.
