---
name: create-ef-migration
description: Plan and, only after explicit approval, generate an AHIS API Template EF Core migration for ApplicationDbContext or IdentityContext without applying it to a database.
---

# Create EF Migration

Read root/module guidance, [persistence documentation](../../../docs/architecture/persistence.md), affected entities/configurations, the correct context, snapshot, and recent migrations. This repository has separate Application and Identity contexts, migrations, and package versions; never guess the context or startup project.

Before generation, provide a Migration Blueprint with context, migration and startup projects, affected tables/columns/indexes/relationships, nullability/defaults/delete behavior, rename-versus-drop/create decision, existing-data and destructive risks, migration name, exact command, and verification plan. Wait for explicit approval.

After approval, generate only the planned migration, inspect every generated operation and snapshot change, then build the affected projects. Never run `dotnet ef database update` unless separately and explicitly authorized for a named target.
