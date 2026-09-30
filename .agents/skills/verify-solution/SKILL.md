---
name: verify-solution
description: Run safe focused-to-broad validation for AHIS API Template changes and report exact results without database, deployment, or external-system mutation.
---

# Verify Solution

Read root instructions and testing strategy. Inspect changed files and choose the narrowest meaningful checks first. Restore only when assets/packages require it; restore, build, and test create local outputs and may use the network.

Preferred sequence:

1. Build the affected project with `--no-restore`.
2. When relevant tests exist, build the affected test project before using `--no-build`, or run `dotnet test` without `--no-build` to avoid stale output.
3. Build `AhisApiTemplate.sln --no-restore`.
4. Run `dotnet test .\AhisApiTemplate.sln --no-build --no-restore`.

The Country handler test is one focused example only; the suite also contains Identity-service, authorization-policy, concurrency, and API-host coverage, some of which require SQL Server LocalDB. Select the closest relevant test or report the coverage gap, then continue with applicable project/solution checks. Report commands, exit codes, test totals, warnings, LocalDB/environmental constraints, and pre-existing failures when evidence permits. Never run database updates, destructive cleanup, production services, or commands requiring live secrets.
