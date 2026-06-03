# Claude Code — SpareParts Workspace Rules

This file is loaded automatically by every Claude Code session (local and remote).
All rules below apply at all times, including when controlled from a mobile device.

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
