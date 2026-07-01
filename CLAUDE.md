# Claude Code — SpareParts Workspace Rules

This file is loaded automatically by every Claude Code session (local and remote).
All rules below apply at all times, including when controlled from a mobile device.

---

## Ralph's Exact-Request Rule (Permanent)

> Always follow Ralph's exact request. Do not improvise, do not change the goal, do not do a lighter version, and do not substitute reports or themes for real implementation.

From now on, always stick exactly to what Ralph asks.

- Do not replace his request with your own idea.
- Do not do something "similar" if he asked for something specific.
- Do not simplify the task unless he asks you to.
- Do not create documents if he asked you to modify code.
- Do not create a new branch unless he explicitly tells you.
- Do not only suggest ideas if he asked you to implement.
- Do not change only the theme if he asked for a full UI/UX rebuild.
- Do not stop after one platform if he asked for WPF, web, and mobile.
- Do not decide that something is "enough" if it does not fully match his request.

Before doing any task, first understand exactly what he asked. For every task, follow this rule:

1. Read his request carefully.
2. Repeat internally what he asked.
3. Check what he specifically told you NOT to do.
4. Do exactly the task.
5. If you are unsure, make the safest assumption that stays closest to his words.
6. Do not go off-track.
7. Do not replace implementation with documentation.
8. Do not make partial changes and call it done.
9. Verify that the final result matches his original request.
10. Remember this instruction for all future tasks in this project.

---

## Design Change Workflow (Permanent)

Whenever Ralph asks for a design/UI/UX change (web, WPF, or mobile):

1. **Before changing anything**: show him picture ideas of the design first (mockups/previews/swatches — e.g. a quick rendered preview or screenshot of the proposed look), not just a text description. Let him react/choose before implementing.
2. **After making the change**: send screenshots of the actual updated app (not just a claim that it changed) proving the new design is live and different from before.
3. Do not skip step 1 or step 2 even for small tweaks — always show before, then after.

---

## Safety Rules (Non-Negotiable)

- **Do NOT delete files** without explicit user approval in the current conversation.
- **Do NOT run destructive commands** (DROP TABLE, DELETE FROM, TRUNCATE, format, rm -rf, etc.).
- **Do NOT run production database commands** (migrations, schema changes, seed data on prod) without explicit approval.
- **Do NOT expose secrets** — never print API keys, connection strings, tokens, passwords, or JWT secrets to chat output.
- **Ask before editing production configs** (appsettings.Production.json, web.config, IIS configs, YARP routing rules).
- **Ask before pushing to Git** — never run `git push` without explicit "yes, push" from the user.
- **Always run build and tests** after making code changes before reporting done.
- **Summarize every changed file** at the end of each task — filename, what changed, why.

---

## Approval-Required Actions

These require a clear "yes" or "approve" response before proceeding:

1. `git push` or `git push --force`
2. Any database migration (`dotnet ef database update`, raw SQL DDL)
3. Deleting or renaming files
4. Modifying `appsettings.Production.json`, `web.config`, or `YARP` route configs
5. Installing new packages (npm, NuGet) — show the package first
6. Running any script not already in the repo
7. Exposing or printing any value that looks like a secret/token/key

---

## Branching & Code Organization Rules

- **Work directly on `main`** — do not create feature/task branches or pull requests. Commit and push changes directly to `main` for all tasks, unless the user explicitly asks for a different branch in that conversation. (The "ask before `git push`" approval rule above still applies.)
- **One type per file, no nested classes** — every class, interface, enum, record, etc. must be defined in its own file (named after the type), never as a nested/inner type inside another class. If existing nested classes are encountered while working in a file, extract them to their own files as part of that change.

---

## Technology Focus

When reviewing, editing, or scanning this repository, prioritize:

- **Backend**: ASP.NET Core 8, JWT authentication, global authorization, CORS policy, error handling middleware
- **Reverse proxy**: YARP / IIS — routing rules, header forwarding, auth passthrough
- **Database**: SQL Server, Dapper — parameterized queries, connection string security, tenant/dbName handling
- **Frontend**: React (web), WPF (desktop), React Native (mobile) — no Angular in this repo
- **Security areas**:
  - Unauthenticated API endpoint exposure
  - Database name / tenant identifier leakage in responses
  - Detailed .NET stack traces returned to clients
  - OPTIONS and TRACE HTTP method handling
  - Security response headers (CSP, X-Frame-Options, HSTS, X-Content-Type-Options)
  - CORS whitelist validation
  - File upload/download authorization
  - Global `[Authorize]` enforcement on all controllers
  - Production-safe error handling (no exception details to clients)

---

## Workflow for Security Scans

When running a security scan:

1. List all files to inspect before touching anything.
2. Read each file, identify the issue, describe it clearly.
3. Show the proposed fix as a diff before applying.
4. Apply the fix only after describing it.
5. Run `dotnet build` after each batch of changes.
6. Run `dotnet test` if tests exist.
7. Report: file changed, issue fixed, test result.

---

## Dev Team Visibility (Permanent)

At the start of every conversation, and whenever agent work is dispatched or its status changes, show the dev team roster and who is currently working on what:

- **Roster**: dev-backend-api, dev-database, dev-desktop-wpf, dev-mobile-rn, dev-web-react, dev-qa-security
- For each agent currently dispatched: show its name, the task it's working on, and its status (running / completed / blocked).
- If no agents are currently running, state that explicitly (e.g. "All dev agents idle") rather than omitting the roster.
- This applies any time work is delegated via the Agent tool to one or more dev-* agents — not just multi-agent fan-outs.

---

## Remote Control Notes

- This workspace can be controlled remotely from a phone via claude.ai/code.
- The same rules apply whether the session is local or remote.
- If reconnecting after a disconnect: run `claude` (or `claude --resume`) in `d:\Ralph\SpareParts`.
- Do not change these CLAUDE.md rules without the user's approval.

---

## Project Structure Quick Reference

```
src/
  SpareParts.Api/           ← Main ASP.NET Core 8 API (all capabilities)
  SpareParts.Infrastructure/ ← Dapper repos, services, accounting
  SpareParts.Domain/        ← Pure domain entities, no framework deps
  SpareParts.Desktop.Wpf/   ← WPF desktop app
  SpareParts.Web.React/     ← React web app (static files + ASP.NET host)
  SpareParts.Mobile.ReactNative/ ← Expo React Native mobile app
tests/
  SpareParts.ArchitectureTests/  ← Layer rules, critical path, security tests
  SpareParts.ManagementTests/    ← Management coordinator tests
  SpareParts.IntegrationTests/   ← Integration tests (SQL Server via Testcontainers)
```
