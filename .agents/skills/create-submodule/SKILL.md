---
name: create-submodule
description: Design and, after approval, add a submodule within an existing business capability while preserving parent ownership and repository conventions.
---

# Create Submodule

Read root and parent-module `AGENTS.md` plus parent docs. Inspect related functionality and determine whether a distinct submodule improves ownership or whether a feature folder/use case is sufficient.

Before editing, report:

- Parent module and proposed submodule purpose
- Data and rule ownership
- Closest existing feature
- Folder and namespace placement
- Whether any new project is necessary (normally no)
- Entity/persistence/API/integration/test/documentation impact
- Explicit service decision
- Exact files to add and modify
- Risks and uncertainties

Wait for explicit architectural approval. After approval, implement using the parent module's patterns, add only meaningful nested instructions/docs, test, and update maps that became inaccurate.
