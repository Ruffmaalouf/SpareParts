# SpareParts — Release & Publish Pipeline Reference

> Audience: any engineer who needs to understand, modify, or debug how SpareParts gets built,
> tested, and deployed. This document describes the pipelines **as they exist in the repository
> today** — it does not describe an aspirational or planned state. Where something does not exist
> yet, it is called out explicitly in "What Does NOT Exist Yet" below.
>
> Source files this document is derived from:
> - `.github/workflows/ci.yml`
> - `.github/workflows/backend-ci.yml`
> - `.github/workflows/wpf-build.yml`
> - `.github/workflows/deploy-staging.yml`
> - `src/SpareParts.Mobile.ReactNative/eas.json`
> - `src/SpareParts.Mobile.ReactNative/app.json`
> - `src/SpareParts.Mobile.ReactNative/package.json`
> - `Directory.Build.props`
>
> Last verified against the repository at commit `3f18059d` (main), 2026-07-02.

---

## 1. Overview — four independent GitHub Actions workflows

All CI/CD in this repo lives under `.github/workflows/`. There are exactly **four** workflow
files. There is no other pipeline (no Azure DevOps, no Jenkins, no GitLab CI) anywhere in the
repo.

| File | Name | Purpose | Runner |
|---|---|---|---|
| `ci.yml` | `ci` | General-purpose PR/push gate: build, format check, architecture tests, SQL Server integration tests | `ubuntu-latest` |
| `backend-ci.yml` | `Backend CI` | Backend-focused build + architecture tests + publishes a downloadable API artifact | `ubuntu-latest` |
| `wpf-build.yml` | `WPF Desktop Build` | Builds and packages the WPF desktop client as a self-contained ZIP | `windows-latest` |
| `deploy-staging.yml` | `deploy-staging` | Provisions Azure infra and deploys API + Web + builds an Android APK — the only workflow that actually deploys anything | `ubuntu-latest` |

These workflows overlap in scope by design (e.g. `ci.yml` and `backend-ci.yml` both build the
backend solution) but trigger on different paths/branches and serve different purposes — `ci.yml`
is the strict quality gate (format-on-diff, warnings-as-errors implicitly via the plain `dotnet
build`), while `backend-ci.yml` is more permissive (`TreatWarningsAsErrors=false`) and additionally
produces a versioned artifact.

---

## 2. `ci.yml` — general CI gate

**Triggers:**
- Every `pull_request` (any base branch)
- Every `push` to `main` or `master`

**Job: `validate`** (runs on `ubuntu-latest`)
1. Checkout with `fetch-depth: 0` (full history — needed for the diff step later).
2. Setup .NET 8.0.x (`actions/setup-dotnet@v4`).
3. Cache NuGet packages keyed on `hashFiles('**/Directory.Packages.props')`.
4. `dotnet restore solutions/SpareParts.Backend.sln`
5. `dotnet build solutions/SpareParts.Backend.sln --configuration Release --no-restore`
6. **Collect changed C# files**: a PowerShell step diffs the current ref against either
   `origin/<base_ref>` (for PRs, after `git fetch origin <base_ref> --depth=1`) or `HEAD~1` (for
   direct pushes), restricted to `*.cs`, `*.csproj`, `*.props`. Produces a multi-line
   `GITHUB_OUTPUT` list of absolute file paths and a `has_changes` flag.
7. **`dotnet format` on touched files only** (conditional on `has_changes == 'true'`): runs
   `dotnet format solutions/SpareParts.Backend.sln --verify-no-changes --severity warn --include
   <files>`. This means formatting is enforced incrementally — only files touched in the current
   PR/push are checked, not the whole solution.
8. **Test suites**: runs only `tests/SpareParts.ArchitectureTests` here (Release, `--no-build`).

**Job: `sql-server-integration`** (runs on `ubuntu-latest`, independent of `validate`)
1. Checkout, setup .NET 8.
2. Cache NuGet packages (same key scheme as above).
3. **Cache the SQL Server Docker image** (`mcr.microsoft.com/mssql/server:2022-latest`) as a tarball
   at `/tmp/sqlserver.tar`, keyed on the fixed string `docker-mssql-server-2022-latest-v1`. On a
   cache hit it's `docker load`-ed; on a miss it's `docker pull`-ed then `docker save`-d back into
   the cache for next time. This avoids re-pulling a large image on every run.
4. Restore + build `tests/SpareParts.IntegrationTests`.
5. Run the integration test suite (uses Testcontainers to spin up SQL Server in the container
   engine on the runner; `TESTCONTAINERS_RYUK_DISABLED: "true"` disables the Testcontainers
   Ryuk reaper container, which is commonly disabled in constrained CI environments where Ryuk's
   Docker-in-Docker requirements aren't available).

This workflow does **not** publish any artifacts and does **not** deploy anything. It is a pure
quality gate.

---

## 3. `backend-ci.yml` — backend build, test, and artifact publish

**Triggers:**
- `push` to `main`, `master`, or `develop`, but only when files under `src/**`, `tests/**`, any
  `*.sln`, `Directory.Build.props`, `Directory.Packages.props`, or the workflow file itself change
  (path filters).
- `pull_request` targeting `main` or `master` (no path filter on PRs).

**Env:** `DOTNET_VERSION: '8.0.x'`, `DOTNET_NOLOGO`, `DOTNET_CLI_TELEMETRY_OPTOUT`.

**Job: `build`** (`ubuntu-latest`)
1. Checkout, setup .NET 8.
2. Cache NuGet (`~/.nuget/packages`, keyed on `Directory.Packages.props` hash).
3. `dotnet restore solutions/SpareParts.Backend.sln`
4. `dotnet build ... --configuration Release --no-restore -p:TreatWarningsAsErrors=false` — note
   this is deliberately **more lenient** than `ci.yml` (warnings do not fail the build here).
5. Run `tests/SpareParts.ArchitectureTests` only — comment in the file explicitly notes
   Architecture Tests run on Linux because they have no WPF dependency.
6. **Integration tests are commented out** in this workflow with an explicit comment: they "need
   SQL Server via Docker — skipped in free CI" and a note to run them locally or in a paid
   environment. (Contrast with `ci.yml`, which *does* run them via the cached Docker image
   approach — so integration test coverage on `main` actually comes from `ci.yml`, not
   `backend-ci.yml`.)
7. Uploads architecture test results as `test-results-backend` (7-day retention), always (even on
   failure, via `if: always()`).

**Job: `publish-api`** (`ubuntu-latest`, `needs: build`)
- Gated to only run `if: github.ref == 'refs/heads/main' || github.ref == 'refs/heads/master'`
  (i.e. never runs on PRs, only on pushes to those two branches).
- Restores and publishes `src/SpareParts.Api/SpareParts.Api.csproj` in Release to
  `./publish/api`.
- Uploads the publish output as a GitHub Actions artifact named
  `spareparts-api-${{ github.sha }}` with **30-day retention**.

This is the only workflow that produces a portable, downloadable, commit-addressable API build
artifact. It is **not** used by `deploy-staging.yml` — the staging deploy does its own independent
`dotnet publish` (see §6). This means `backend-ci.yml`'s artifact is informational/for manual
download only, not part of the staging deploy chain.

---

## 4. `wpf-build.yml` — WPF desktop build & packaging

**Triggers:**
- `push` to `main`, `master`, or `develop`, path-filtered to `src/SpareParts.Desktop.**`,
  `src/SpareParts.Domain/**`, `src/SpareParts.Infrastructure/**`, `src/SpareParts.Application/**`,
  `Directory.Build.props`, `Directory.Packages.props`, or the workflow file itself.
- `pull_request` targeting `main` or `master` (no path filter).

**Job: `build-wpf`** (`windows-latest` — the only workflow in the repo that needs a Windows
runner, because WPF requires `net8.0-windows` and Windows-only build tooling)

1. Checkout, setup .NET 8.
2. Cache NuGet.
3. `dotnet restore solutions/SpareParts.Desktop.sln`
4. `dotnet build ... --configuration Release --no-restore -p:TreatWarningsAsErrors=false`
5. **Publish self-contained**: `dotnet publish src/SpareParts.Desktop.Wpf/SpareParts.Desktop.Wpf.csproj
   --configuration Release --runtime win-x64 --self-contained true --output ./publish/wpf
   -p:PublishSingleFile=false -p:TreatWarningsAsErrors=false`. This bundles the .NET 8 runtime into
   the output so end users do not need the .NET runtime pre-installed. `PublishSingleFile=false`
   means the output is a folder of many files (DLLs, exe, native deps), not a single consolidated
   `.exe`.
6. **Zip the output**: `Compress-Archive` into
   `./publish/SpareParts-Desktop-<first-8-chars-of-sha>.zip`.
7. Run `tests/SpareParts.ManagementTests` (Windows-only test suite — depends on WPF/Windows APIs),
   with `continue-on-error: true` — a failure here does **not** fail the workflow.
8. Uploads three artifacts:
   - `test-results-management` (7-day retention) — always uploaded (`if: always()`).
   - `spareparts-desktop-<sha>` — the zip file, 30-day retention.
   - `spareparts-desktop-files-<sha>` — the unzipped `publish/wpf/` folder, 14-day retention
     (useful for inspecting individual files without downloading/extracting the zip).

There is no code-signing step, no installer generation (MSI/MSIX/Inno Setup/WiX), and no
publishing to any distribution channel (no Microsoft Store, no internal update feed). See §7 for
what this means in practice.

---

## 5. `deploy-staging.yml` — the only deployment workflow

**Triggers:**
- `push` to `main` (i.e. every merge/direct push to main triggers a full staging redeploy).
- `workflow_dispatch` with two optional inputs:
  - `reset_db` (`false`/`true`, default `false`) — if `true`, drops and recreates the staging
    database, **wiping all data**.
  - `recreate_plan` (`false`/`true`, default `false`) — if `true` and the existing App Service Plan
    is Linux, deletes the plan and the API/Web apps and recreates everything as Windows F1 (free
    tier). Without this flag, the provision job hard-fails if it finds an existing Linux plan
    (rather than silently mutating infrastructure).

**Environment variables** (shared across all jobs via top-level `env:`):
```
RESOURCE_GROUP: spareparts-rg
LOCATION: eastus
PLAN_NAME: spareparts-plan
API_APP_NAME: spareparts-api-ralph
WEB_APP_NAME: spareparts-web-ralph
SQL_SERVER_NAME: spareparts-sql-ralph
SQL_DB_NAME: SparePartsDb
SQL_ADMIN_USER: spadmin
```

This workflow has **no `environment:` protection rule configured** in the YAML (no manual approval
gate) — every push to `main` runs it automatically end-to-end, including the Azure resource
provisioning and the actual `azure/webapps-deploy` deployment steps.

### 5.1 Job graph

```
provision ──┬──> deploy-api ──┬──> deploy-web
            │                 │
            │                 └──> build-android (continue-on-error: true)
            │
            └───────────────────────────────────────┐
                                                      ▼
                                                   summary (always runs, needs all four)
```

- `deploy-api`, `deploy-web`, and `build-android` all depend on `provision` (via its `outputs`).
- `deploy-web` and `build-android` both additionally depend on `deploy-api` (so the API is live —
  and its URL known/health-checked — before Web is deployed or the mobile app is built with that
  URL baked in).
- `build-android` has `continue-on-error: true` — an Android build failure does not fail the whole
  workflow (API/Web deploys are treated as the critical path; mobile is best-effort in this
  particular workflow).
- `summary` always runs (`if: always()`) once all upstream jobs have finished (succeeded, failed,
  or skipped) and prints a plain-text summary of URLs and each job's result.

### 5.2 `provision` job — end-to-end

Runs on `ubuntu-latest`. Steps, in order:

1. **Checkout.**
2. **Azure Login** (`azure/login@v2`) using a service principal built from four repo secrets:
   `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID`.
3. **Generate SQL password**: `SpareParts!<6-digit-random>Rg<14-hex-chars>`, masked in logs via
   `::add-mask::`. This password is generated **fresh on every run** — it is not persisted between
   runs except by being written into Azure App Service settings and the SQL Server admin password
   each time (see step 8/9). This means every push to `main` rotates the SQL admin password.
4. **Set URL outputs**: computes `api_url`, `web_url` (both fixed
   `https://<app-name>.azurewebsites.net` — deterministic, not randomly suffixed, unlike what
   `docs/deployment-plan.md` describes for the standalone PowerShell script path) and the full SQL
   `connection_string`, all exposed as job outputs for downstream jobs. The connection string is
   also masked.
5. **Register resource providers**: `Microsoft.Sql`, `Microsoft.Web` (idempotent, waits for
   registration).
6. **Create resource group** (`spareparts-rg` in `eastus`, idempotent — `az group create` is a
   no-op if it already exists).
7. **Create App Service Plan** — this step contains real branching logic:
   - If a plan named `spareparts-plan` already exists and is **Linux**, the workflow either (a)
     fails outright with an explanatory error, or (b) if `recreate_plan=true` was passed via
     `workflow_dispatch`, deletes the API app, the Web app, and the plan, then proceeds to
     recreate as Windows F1.
   - If no plan exists (or it was just deleted), the workflow tries a **list of regions in order**
     (`westeurope, northeurope, eastus, eastus2, westus2, centralus, southeastasia`) attempting
     `az appservice plan create --sku F1` in each until one succeeds, to work around F1
     (free-tier) quota/availability being inconsistent per-region on a given subscription. The
     succeeding region is captured into `$GITHUB_ENV` as `DEPLOY_REGION` for later steps (e.g. SQL
     Server creation uses the same region).
   - If an existing plan is found and is not Linux (i.e. already Windows), its region is looked up
     and reused as `DEPLOY_REGION`.
8. **Create API Web App** / **Create Web App** — idempotent creates (`if ! az webapp show ...`)
   using `--runtime "dotnet:8"` on the shared plan.
9. **Create SQL Server** — idempotent create; if the server already exists, instead **updates its
   admin password** to the freshly generated one from step 3 (this is the password-rotation
   behavior noted above).
10. **Allow Azure services to reach SQL** — firewall rule `AllowAzureServices`,
    `0.0.0.0`–`0.0.0.0` (the special Azure-only sentinel range, not "open to the internet").
11. **Create SQL Database (or reset if requested)** — idempotent create unless `reset_db=true` was
    passed, in which case the existing DB is deleted first. New DB is `GeneralPurpose` / `Gen5` /
    2 vCore / **Serverless** compute model, 32GB max size, `--use-free-limit` with
    `AutoPause` behavior on free-limit exhaustion, locally-redundant backups.
12. **Run database schema** — installs `sqlcmd` via `mssql-tools18`, waits for SQL reachability
    (up to 12 attempts × 15s = 3 minutes), then applies `database/schema.sql` via `sqlcmd -i`.
    **This is the step fixed in Round 2 — see §8 below for the before/after.**
13. **Configure API app settings** — generates a fresh JWT secret (`openssl rand -base64 48`,
    masked) and sets, via `az webapp config appsettings set`:
    `ASPNETCORE_ENVIRONMENT=Staging`, `ConnectionStrings__DefaultConnection`, `Jwt__Secret`,
    `Jwt__Issuer=SpareParts.Api`, `Jwt__Audience=SpareParts.Desktop`,
    `Cors__AllowedOrigins__0=<web-app-url>`, `Cors__AllowedOrigins__1=http://localhost:5078`,
    `SCM_DO_BUILD_DURING_DEPLOYMENT=false`. Optionally also sets
    `ExternalAuth__GoogleClientId`, `ExternalAuth__FacebookAppId`,
    `ExternalAuth__FacebookAppSecret` if the corresponding repo secrets are non-empty (note the
    `GOOGLE_WEB_CLIENT_ID || GOOGLE_CLIENT_ID` fallback). Also enables filesystem application and
    web-server logging on the API app.
14. **Configure Web app settings** — sets `ASPNETCORE_ENVIRONMENT=Staging` and
    `SCM_DO_BUILD_DURING_DEPLOYMENT=false` on the Web app.

Every JWT secret and SQL password rotation on each run means **existing JWTs issued before a given
`main` push become invalid after that push's deploy completes** — this is documented behavior, not
a bug, but worth knowing when debugging "why did staging log everyone out."

### 5.3 `deploy-api` job

Runs on `ubuntu-latest`, `needs: provision`.
1. Checkout, setup .NET 8.
2. `dotnet publish src/SpareParts.Api/SpareParts.Api.csproj -c Release -o artifacts/api` (a fresh,
   independent publish — not reusing `backend-ci.yml`'s artifact).
3. Azure Login (same 4-secret service principal pattern as `provision`).
4. **Deploy API** via `azure/webapps-deploy@v3`, pushing `artifacts/api` to
   `spareparts-api-ralph`.
5. **Wait for API health check** — polls `GET <api_url>/api/health` every 15s for up to 36
   attempts (9 minutes) looking for HTTP 200. On success, exits 0 immediately. On timeout:
   downloads the App Service log ZIP (`az webapp log download`), extracts it, prints all found log
   file paths, tails the last 300 lines of the most recent `.log`/`.txt` files, greps for
   exception/error/SQL-failure patterns across all logs (filtering out benign `ContainerTimeout`/
   `cold start` noise), and — if no log archive was downloadable — falls back to streaming live
   logs for 20 seconds via `az webapp log tail`. Then exits 1 (fails the job) regardless.

### 5.4 `deploy-web` job

Runs on `ubuntu-latest`, `needs: [provision, deploy-api]` — i.e. Web is only built/deployed after
the API is confirmed healthy, because the Web build needs the confirmed API URL and it makes no
sense to point the web app at a dead API.
1. Checkout, setup .NET 8.
2. **Patch `config.js` with the API URL**: overwrites
   `src/SpareParts.Web.React/wwwroot/config.js` in the checked-out workspace (not committed —
   this is a build-time-only mutation inside the ephemeral runner) with
   `window.SparePartsWebConfig = { defaultApiBaseUrl: "<api_url>", googleClientId: "...",
   facebookAppId: "..." }`.
3. `dotnet publish src/SpareParts.Web.React/SpareParts.Web.React.csproj -c Release -o
   artifacts/web`.
4. Azure Login.
5. **Deploy Web** via `azure/webapps-deploy@v3` to `spareparts-web-ralph`.

There is no post-deploy health check for the Web app (unlike the API's `/api/health` polling
loop) — this is a gap; see §7.

### 5.5 `build-android` job

Runs on `ubuntu-latest`, `needs: [provision, deploy-api]`, `continue-on-error: true` (failures
here do not fail the overall workflow run, though they are visible in the `summary` job's printed
`build-android.result`).

1. Checkout.
2. Setup Node 22 with npm cache keyed on `src/SpareParts.Mobile.ReactNative/package-lock.json`.
3. `npm ci` in the mobile project directory.
4. **Write `.env`** with `EXPO_PUBLIC_API_BASE_URL=<api_url>` plus Google/Facebook client ID/secret
   values sourced from repo secrets (`GOOGLE_CLIENT_ID`, `GOOGLE_ANDROID_CLIENT_ID`,
   `GOOGLE_IOS_CLIENT_ID`, `GOOGLE_WEB_CLIENT_ID`, `FACEBOOK_APP_ID`).
5. **Configure EAS staging API URL**: a Node one-liner reads `eas.json`, ensures
   `build.staging.env.EXPO_PUBLIC_API_BASE_URL` is set to the just-deployed staging API URL (and
   backfills the optional Google/Facebook env vars if present), and rewrites `eas.json` in place
   on the runner's checkout (again, not committed back to the repo — this mutation only lives for
   the duration of the job).
6. **Verify no localhost**: greps the written `.env` for `localhost`/`127.0.0.1`/`10.0.2.2` and
   fails if found; then a second Node check parses `eas.json`'s `build.staging.env.
   EXPO_PUBLIC_API_BASE_URL` and fails if it's empty or matches the same local-host patterns. This
   is a safety net to guarantee a staging APK is never accidentally built pointing at a
   developer's local machine.
7. **Install EAS CLI**: `npm install -g eas-cli@16` (pinned major version — a Round 1 fix per the
   task history).
8. **Build Android APK via EAS**: `eas build --platform android --profile staging
   --non-interactive`, authenticated via the `EXPO_TOKEN` repo secret. This uses Expo's **cloud**
   build service (EAS Build) — the GitHub Actions runner itself does not compile the native
   Android app; it only triggers and (implicitly, via `eas-cli`) waits on Expo's servers, then the
   job ends. The resulting APK is retrievable from the Expo/EAS dashboard (`https://expo.dev`),
   not as a GitHub Actions artifact — this workflow does not download or attach the built APK
   anywhere in GitHub.

### 5.6 `summary` job

Runs on `ubuntu-latest`, `needs: [provision, deploy-api, deploy-web, build-android]`,
`if: always()`. Purely prints API/Web/Health/Swagger URLs and the `build-android` job's `.result`
(`success`/`failure`/`skipped`/`cancelled`) to the workflow log. Not a gate, not a notification —
just a human-readable end-of-run summary in the Actions log output.

---

## 6. Full staging deploy flow — API, Web, Android (end-to-end narrative)

This is the practical sequence of what happens when someone pushes to `main` (or manually
dispatches the workflow):

1. GitHub triggers `deploy-staging.yml`.
2. **Provisioning** (idempotent — safe to re-run): resource group, App Service Plan (Windows F1,
   trying multiple regions if needed), two Web Apps (API + Web) on that plan, an Azure SQL logical
   server + serverless database, a firewall rule allowing Azure-internal traffic only, then the
   database schema is applied via `sqlcmd` against `database/schema.sql`. App settings (JWT
   secret, connection string, CORS origins, optional OAuth client IDs) are written to the API app;
   minimal settings are written to the Web app. SQL admin password and JWT secret are freshly
   generated **every single run**.
3. **API deploy**: the API project is published (`dotnet publish`, Release) and pushed to the API
   App Service via `azure/webapps-deploy@v3`. The workflow then blocks for up to 9 minutes polling
   `/api/health` until it returns 200 — if it never does, logs are pulled and the job fails loudly.
4. **Web deploy** (only after API is confirmed healthy): `wwwroot/config.js` is patched in-place
   with the live API URL and OAuth client IDs, then the Web project is published and pushed to the
   Web App Service the same way. No health check follows.
5. **Android build** (only after API is confirmed healthy, in parallel with/independent of the Web
   deploy since both just depend on `deploy-api`): mobile dependencies installed, `.env` and
   `eas.json`'s staging profile are both pointed at the live staging API URL, a safety check
   verifies no `localhost` leaked in, then `eas build` is kicked off against Expo's cloud build
   service for an internal-distribution APK. This step can fail without failing the whole
   workflow.
6. **Summary**: prints all resulting URLs and each job's outcome.

There is currently **no iOS build** anywhere in `deploy-staging.yml` (`eas.json` has iOS
build config implicitly via Expo defaults and `app.json` has an `ios` block, but no workflow step
invokes `eas build --platform ios`). iOS is documented as a manual/local-only path in
`docs/deployment-plan.md` Part 8 (requires a Mac + Apple Developer account, out of scope for this
CI).

---

## 7. Versioning scheme

### 7.1 .NET side — `Directory.Build.props`

`Directory.Build.props` (repo root) is automatically picked up by every `.csproj` under the repo
(MSBuild convention — it's imported into every project in the directory tree unless a project
opts out). It currently pins:

```xml
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

This is a **Round 1 fix** (per the in-file comment) — previously every project defaulted to the
implicit `1.0.0.0` with no shared source of truth, meaning API, Web host, and WPF binaries could
silently drift out of version sync with no build-time signal. Now:
- `src/SpareParts.Api/SpareParts.Api.csproj` — no local `<Version>` override, inherits `1.0.0` from
  `Directory.Build.props` (verified: no `Version` property in the csproj).
- `src/SpareParts.Web.React/SpareParts.Web.React.csproj` — same, inherits `1.0.0`.
- `src/SpareParts.Desktop.Wpf/SpareParts.Desktop.Wpf.csproj` — same, inherits `1.0.0` (target
  framework is `net8.0-windows`, the only Windows-specific TFM in the solution).
- Test projects get `<IsPackable>false</IsPackable>` via the `IsTestProject` condition in the same
  file, unrelated to versioning but co-located in the same props file.

`Directory.Build.props` also sets solution-wide `<Nullable>enable</Nullable>`,
`<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latestmajor</LangVersion>`, and
`<Deterministic>true</Deterministic>` (reproducible builds — same source produces byte-identical
output, which matters for build caching and supply-chain verification).

### 7.2 Mobile side — `app.json` and `package.json`

`src/SpareParts.Mobile.ReactNative/app.json` sets `expo.version: "1.0.0"` — this is the
user-facing app version shown in app stores and is what `expo-updates`/EAS uses for the
"runtime version" concept in some configurations (not configured here beyond the plain version
string). `src/SpareParts.Mobile.ReactNative/package.json` independently also has
`"version": "1.0.0"` (the npm package version — largely cosmetic for a private, non-published
package, but conventionally kept in sync with `app.json`).

`eas.json`'s top-level `cli.appVersionSource: "remote"` means **EAS itself manages/increments the
native build version numbers (Android `versionCode`, iOS `buildNumber`) remotely on Expo's
servers**, separate from the `expo.version` string in `app.json`. This is an EAS-native mechanism,
not something `Directory.Build.props` or the GitHub workflow touches.

### 7.3 How the two are coordinated (as of this writing)

**They are coordinated by convention/comment only, not by tooling.** The comment in
`Directory.Build.props` says explicitly:

> "Keep this in sync with the mobile app version in
> `src/SpareParts.Mobile.ReactNative/app.json` and `package.json` whenever a cross-platform
> release ships."

Today, both sides happen to read `1.0.0` — but nothing in any workflow asserts they match, and
nothing bumps them together automatically. A "release" in this repo, practically, means:
1. Manually edit `Directory.Build.props`'s `<Version>`/`<AssemblyVersion>`/`<FileVersion>` (bumps
   API, Web, and WPF simultaneously since they all inherit from this one file).
2. Manually edit `app.json`'s `expo.version` and `package.json`'s `version` to the same semantic
   version.
3. Push to `main`, which triggers `deploy-staging.yml` (API/Web/Android to staging) and
   `wpf-build.yml`/`backend-ci.yml` (artifacts) automatically via their path triggers.
4. There is no single workflow or script that bumps all of these files together — see §7.4.

### 7.4 Gap: no automated version-bump or version-consistency check

There is no workflow step anywhere (in any of the four YAML files) that:
- Compares `Directory.Build.props`'s `<Version>` against `app.json`'s `expo.version` and fails if
  they differ.
- Auto-increments any of these values based on tags, commits, or semantic-release conventions.
- Stamps the built artifacts' filenames with the semantic version (WPF's zip is named by **git
  SHA prefix**, not by `Directory.Build.props`'s version — see `wpf-build.yml`'s "Zip WPF output"
  step: `$version = "${{ github.sha }}".Substring(0, 8)`; `backend-ci.yml`'s API artifact is
  likewise named `spareparts-api-${{ github.sha }}`, not by semantic version).

This means today, "the version" is really tracked in two places by hand, and the artifact names
you actually see in GitHub Actions (zip files, artifact bundles) are keyed by commit SHA, not by
the `1.0.0` semantic version. Both are useful (SHA for exact traceability, semantic version for
human-facing "what release is this") but they are not cross-referenced anywhere in tooling.

---

## 8. Round 2 fix: `deploy-staging.yml` "Run database schema" step — deprecated `apt-key`

### Before (Round 1 left this unresolved)

```bash
echo "Installing sqlcmd..."
curl -s https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add - 2>/dev/null || true
curl -s https://packages.microsoft.com/config/ubuntu/22.04/prod.list | sudo tee /etc/apt/sources.list.d/mssql-release.list
sudo apt-get update -qq
ACCEPT_EULA=Y sudo apt-get install -y -qq mssql-tools18 unixodbc-dev
```

Problems:
1. `apt-key add` is deprecated by Debian/Ubuntu (removed entirely in newer releases) in favor of
   per-repository keyrings referenced via `signed-by=`.
2. The Microsoft key download was piped straight into `apt-key add`, and **any failure in that
   pipeline (network error, key format change, `apt-key` being absent entirely on newer
   images) was silently swallowed** by `2>/dev/null || true` — the workflow would proceed to
   `apt-get update` even if the key was never actually trusted, potentially failing later with a
   confusing "not signed" error instead of a clear "key install failed" error, or in the worst
   case installing from an unauthenticated source if `apt-get` was configured to allow it.
3. The apt source list hardcoded `ubuntu/22.04/prod.list` while `ubuntu-latest` now resolves to
   **24.04 (noble)**, not 22.04 (jammy). Using the jammy package list on a noble runner works by
   coincidence today (Microsoft's `.list` files are simple enough that a mismatch often still
   resolves) but is not guaranteed to keep working and is not correct.

### After (fixed now)

```bash
echo "Installing sqlcmd..."
curl -sfo /tmp/microsoft.asc https://packages.microsoft.com/keys/microsoft.asc
sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg /tmp/microsoft.asc
rm -f /tmp/microsoft.asc
echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/24.04/prod noble main" \
  | sudo tee /etc/apt/sources.list.d/mssql-release.list
sudo apt-get update -qq
ACCEPT_EULA=Y sudo apt-get install -y -qq mssql-tools18 unixodbc-dev
```

What changed and why:
- `curl -sfo` (added `-f`) — fails the step immediately (non-zero exit) if the key can't be
  downloaded, instead of continuing with an empty/partial file.
- `gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg` — the modern replacement for
  `apt-key add`; writes a binary keyring scoped to this one repository rather than adding to the
  system-wide trusted keyring (which `apt-key` did, and which is exactly why `apt-key` was
  deprecated — a compromised or stale key in the global keyring affects trust decisions for every
  repo on the system, not just Microsoft's).
- No more `2>/dev/null || true` anywhere in this block — every command's exit code now actually
  gates the rest of the job.
- The apt source list entry now explicitly declares `[signed-by=/usr/share/keyrings/microsoft-prod.gpg]`
  — required as soon as the key is no longer in the global `apt-key` keyring; without this,
  `apt-get update` would reject the repo as unsigned.
- Source URL changed from `ubuntu/22.04/prod.list`'s contents (jammy) to the verified,
  Microsoft-published `ubuntu/24.04/prod noble main` line, matching what `ubuntu-latest` actually
  runs. This was verified live: `curl -s https://packages.microsoft.com/config/ubuntu/24.04/prod.list`
  returns `deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg]
  https://packages.microsoft.com/ubuntu/24.04/prod noble main` — Microsoft does publish a noble
  source, so no jammy-fallback logic was needed.

No other file in the repository used the `apt-key` pattern (verified via repo-wide search,
excluding `node_modules`), so this was the only occurrence to fix.

---

## 9. What does NOT exist yet (accurate, verified gaps)

This section exists specifically so nobody assumes something is automated/covered when it isn't.

1. **No production deploy workflow.** There is no `deploy-production.yml` or equivalent anywhere
   in `.github/workflows/`. `deploy-staging.yml` is the only deployment workflow in the repo, and
   it only targets the `spareparts-*-ralph` staging App Services and `SparePartsDb` on
   `spareparts-sql-ralph`. Promoting staging to a production environment today would need to be
   done by hand (or a new workflow written) — there is no slot-swap, no separate production
   resource group, no production secrets wired up in this repo's workflows.
2. **No real WPF installer.** `wpf-build.yml` produces a self-contained **ZIP** of the publish
   output (`SpareParts-Desktop-<sha>.zip`). There is no MSI, MSIX, ClickOnce, Inno Setup, WiX, or
   Squirrel installer anywhere in the repo (`src/SpareParts.Desktop.Wpf/` has no `.wixproj`,
   `.iss`, publish profile `.pubxml`, or installer manifest — verified via search). End users would
   need to manually unzip and run the `.exe`; there's no Start Menu shortcut creation, no
   uninstaller, no auto-update mechanism, and no code signing.
3. **No code signing for WPF or mobile.** The WPF zip is unsigned. The Android APK is built via
   EAS with Expo-managed credentials (per `docs/deployment-plan.md` Part 6) — adequate for internal
   testing/sideloading but not configured for Play Store release signing in this repo's tracked
   config.
4. **No iOS build in CI.** `eas.json`/`app.json` have iOS-relevant config (`bundleIdentifier`,
   `supportsTablet`), but no workflow anywhere invokes `eas build --platform ios`. iOS builds are
   documented as a manual, local, Mac-required process in `docs/deployment-plan.md` Part 8, not
   automated.
5. **No automated cross-platform version-consistency check.** As described in §7.4, nothing
   verifies `Directory.Build.props`'s version matches `app.json`'s/`package.json`'s version, and
   nothing bumps them together.
6. **No health check for the Web app after deploy.** `deploy-api` polls `/api/health` for up to 9
   minutes with log retrieval on failure; `deploy-web` has no equivalent verification step — a
   broken Web deploy would only surface via `summary`'s (non-blocking) printout or manual
   inspection.
7. **No artifact retrieval/attachment for the Android APK.** `build-android` triggers an EAS cloud
   build and exits; the resulting APK is never downloaded into the GitHub Actions run or attached
   as a workflow artifact. It only exists on Expo's servers (`https://expo.dev`), reachable via the
   `EXPO_TOKEN`-authenticated account.
8. **No rollback automation.** `docs/deployment-plan.md` documents manual rollback commands (BACPAC
   restore, redeploying a previous zip, `adb install --replace`), but none of this is wired into
   any workflow — it is entirely a manual, human-run process today.
9. **No `environment:` protection / manual-approval gate on `deploy-staging.yml`.** Every push to
   `main` deploys to staging automatically and immediately; there is no GitHub Environments
   approval step configured in the workflow file.
10. **`backend-ci.yml`'s published API artifact is not consumed anywhere.** It's uploaded to GitHub
    Actions for manual download only; `deploy-staging.yml` does its own separate `dotnet publish`
    rather than reusing it.

---

## 10. Secrets inventory (names only — never print values)

Referenced across the workflows (names as they appear in `secrets.*` expressions):

| Secret | Used in | Purpose |
|---|---|---|
| `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID` | `deploy-staging.yml` (`provision`, `deploy-api`, `deploy-web`) | Service principal for `azure/login@v2` |
| `GOOGLE_WEB_CLIENT_ID` (falls back to `GOOGLE_CLIENT_ID`) | `deploy-staging.yml` (`provision`, `deploy-web`) | Google OAuth web client ID for API/Web config |
| `GOOGLE_CLIENT_ID`, `GOOGLE_ANDROID_CLIENT_ID`, `GOOGLE_IOS_CLIENT_ID` | `deploy-staging.yml` (`build-android`) | Google OAuth client IDs baked into the mobile `.env`/EAS profile |
| `FACEBOOK_APP_ID` | `deploy-staging.yml` (`provision`, `deploy-web`, `build-android`) | Facebook login app ID |
| `FACEBOOK_APP_SECRET` | `deploy-staging.yml` (`provision`) | Facebook login app secret (API only, never sent to Web/mobile) |
| `EXPO_TOKEN` | `deploy-staging.yml` (`build-android`) | Authenticates `eas build` against the Expo/EAS account |

No secrets are referenced in `ci.yml`, `backend-ci.yml`, or `wpf-build.yml` — those three
workflows only build/test, they never touch external credentials.

---

## 11. Quick-reference: which workflow runs when

| Event | `ci.yml` | `backend-ci.yml` | `wpf-build.yml` | `deploy-staging.yml` |
|---|---|---|---|---|
| PR opened (any base) | Yes | Yes, if PR targets `main`/`master` | Yes, if PR targets `main`/`master` | No |
| Push to `main` | Yes | Yes, if backend paths changed | Yes, if WPF/domain/infra paths changed | Yes, always |
| Push to `master` | Yes | Yes, if backend paths changed | Yes, if WPF/domain/infra paths changed | No |
| Push to `develop` | No | Yes, if backend paths changed | Yes, if WPF/domain/infra paths changed | No |
| Manual (`workflow_dispatch`) | No (not enabled) | No (not enabled) | No (not enabled) | Yes, with `reset_db`/`recreate_plan` options |

Note `deploy-staging.yml` is the only workflow with `workflow_dispatch` enabled, and the only one
that triggers on `push: branches: [main]` unconditionally (no path filter) — every merge to `main`,
regardless of what changed, kicks off a full staging redeploy attempt (including infra
provisioning checks, which are cheap/idempotent when nothing changed).
