---
name: implement-approved-change
description: Implement an approved AHIS API Template blueprint with one writer, scope-drift control, proportional validation, and targeted documentation maintenance.
---

# Implement Approved Change

Confirm the active request or approved blueprint covers the exact material effects. Read the root and owning-module instructions, inspect current status, and preserve unrelated user changes. One writer owns implementation files.

Implement the smallest coherent change using the documented module pattern. Stop and revise the blueprint if discovery reveals a material API, behavior, security, authorization, schema, migration, dependency, integration, or production-configuration change.

Run focused-to-broad validation appropriate to the diff. Update only documentation made inaccurate by the approved change, retain evidence/confidence labels, and report modified files, validation, warnings, remaining risks, and work not performed. Never apply a migration, deploy, publish, push, or mutate an external system without separate authorization.
