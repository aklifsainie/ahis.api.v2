---
name: create-entity
description: Analyze and, after approval, create or modify a persisted entity with the correct ownership, base type, EF configuration, auditing, and migration implications.
---

# Create Entity

Read root/module instructions, persistence docs, the closest entity, base entity, EF configuration, context, and migration snapshot.

Before editing, report:

- Owning module and aggregate/entity justification
- Correct project and namespace
- Key generation, base type, audit/soft-delete/status fields
- Properties, nullability, defaults, navigation properties, foreign keys, delete behavior, indexes, uniqueness, precision/conversions, and sensitive-data marking
- Context and repository impact
- Migration/data risks
- ViewModel/API/rule/test/documentation impact
- Exact files to add/modify

Produce an architectural blueprint and wait for explicit approval. Do not generate a migration as part of entity creation unless a separate approved migration blueprint authorizes it.
