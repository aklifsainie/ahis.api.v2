---
name: verify-solution
description: Discover and run safe focused-to-broad build and test checks for this .NET solution, reporting warnings and failures without destructive database operations.
---

# Verify Solution

Read root `AGENTS.md` and testing strategy. Inspect the solution/project paths and changed files to choose focused checks.

Preferred order:

1. Restore only when dependencies/assets require it.
2. Build the affected project with `--no-restore`.
3. Run affected tests with `--no-build --no-restore` when the build output is current.
4. Build `AhisApiTemplate.sln`.
5. Test `AhisApiTemplate.sln`.

Use the actual configuration needed by the task. Report commands, exit status, tests passed/failed/skipped, and relevant warnings. Separate pre-existing warnings from newly introduced ones when evidence permits.

Do not run database updates, destructive cleanup, production services, or commands requiring live secrets. Migration verification may inspect/build generated code but must not apply it.
