# CLAUDE.md

Guidance for agents working in this repository.

## Repo map

| Path | Purpose |
|------|---------|
| `src/Auth/Program.cs` | ASP.NET Core entry point: builder/service wiring, middleware pipeline, NLog setup. |
| `src/Auth/Controllers` | REST API controllers — `UsersController`, `RolesController`, `PermissionsController`, `ResourcesController`, `ActionsController`. |
| `src/Auth/Services` | Business logic behind the controllers (`UserService`, `RoleService`, `PermissionService`, `ResourceService`, `ActionService`) and their interfaces. |
| `src/Auth/Data` | `AuthDbContext`; repository contracts (`Data/Contracts`) and implementations (`Data/Repositiories`, sic); EF Core migrations (`Data/Migrations`). |
| `src/Auth/Models` | DTOs (`Models/Dto`), EF Core entities and fluent configurations (`Models/Entities`, `Models/Entities/Configurations`), hand-written entity/DTO mappers and their `IEntityMapper` adapters (`Models/Mappings`), options classes (`Models/Config`). |
| `src/Auth/Common` | Cross-cutting concerns: paging/query-filter abstractions (`Common/Data`), domain exceptions plus the global exception middleware (`Common/Exceptions`), model-validation filters (`Common/Filters`), extension methods (`Common/Extensions`), display-name helpers (`Common/Helpers`), shared DTO/entity base types (`Common/Models`), query/paging mappers (`Common/Mappings`), and the generic `CrudService<TEntity, TResponseDto, TDetailResponseDto>` base plus its `IEntityMapper` abstraction (`Common/Services`). |
| `src/Auth/Properties/launchSettings.json` | Local `dotnet run` launch profiles. |
| `docker/` | Files copied into the runtime image: `entry.sh` (container entrypoint), `update-cacerts.sh`, `static-functions`. |
| `Dockerfile` | Multi-stage build (Alpine-based .NET 9 SDK/ASP.NET images) producing the `ilm/auth` runtime image. |

There is no test project — `Auth.sln` reserves a `tests` solution folder, but it has no project in it yet.

## Commands

- Restore: `dotnet restore`
- Build: `dotnet build --no-restore`
- Docker image: `docker build -f Dockerfile -t ilm/auth .`

Run restore + build from the repo root before considering a change done. There is no test project, so there are no unit/integration test commands to run yet.

## Conventions

- Every third-party GitHub Action reference is pinned to a full commit SHA
  with the human-readable version as a trailing comment, e.g.
  `owner/action@<full-sha> # vX.Y.Z`. Never reference a third-party action
  by a mutable tag or branch. Org-internal `OmniTrustILM/.github` reusable
  workflows (`containers-test.yml`, `containers-build-and-push.yml`) are the
  deliberate exception: they stay on `@main` (org-controlled).
- Commit messages are one plain, descriptive sentence. No co-author
  trailers and no mention of AI assistance or tooling.
- Don't leave short, all-caps stand-in markers (TODO/FIXME) in code or
  comments to flag unfinished work. Either finish the work in the same
  change, or track it as an issue and reference the issue number in prose.

## Quality gates

- **SonarCloud** — project key `OmniTrustILM-auth`, organization `ilm`
  (verified live against the SonarCloud API; the older `CZERTAINLY_CZERTAINLY-Auth`
  key from the pre-rename org no longer resolves). SonarScanner for .NET does
  **not** read `sonar-project.properties` — configuration instead lives in
  three places, split by what it is:
  - **Scan-scope settings** (host URL, coverage-report path, coverage
    exclusions) live in the committed `SonarQube.Analysis.xml` at the repo
    root, passed to the scanner via `dotnet sonarscanner begin
    /s:"$GITHUB_WORKSPACE/SonarQube.Analysis.xml"` (`/s:` requires an
    absolute path). Add a per-project exclusion via the `SonarQubeExclude`
    MSBuild property on that project when one is ever needed.
  - **The project key AND the organization are command-line-only scanner
    arguments** (`/k:"OmniTrustILM-auth"` and `/o:"ilm"`) — SonarScanner for
    .NET's own `SonarQube.Analysis.xml` template explicitly documents that
    `sonar.projectKey`, `sonar.projectName`, `sonar.projectVersion`, and
    `sonar.organization` cannot be set via that file or an MSBuild project
    file, only via `/k:`/`/n:`/`/v:`/`/o:`. The Sonar token and per-PR
    params (`pullrequest.key`, `.branch`, `.base`, `scm.revision`) are also
    `/d:` CLI arguments, never committed — the token is a secret and the PR
    params are per-run values.
  - **Quality gate configuration, quality profiles, and the "new code"
    definition** live entirely in the SonarCloud UI, not in this repo.
  `build.yml` (push to `main*`) is compile-only and runs no Sonar step.
  Sonar analysis runs through two separate paths instead:
  - **Pull requests** — a four-workflow, fork-safe chain modeled on
    `OmniTrustILM/pyadcs-connector`'s pattern:
    `check_pr.yml` (builds, runs `dotnet test` with
    `--collect:"XPlat Code Coverage;Format=opencover"`, and uploads a
    `coverage-report` artifact — always non-empty, via a
    `coverage/no-coverage-placeholder.txt` placeholder, even before a test
    project exists, plus a `pr-context` artifact; the placeholder must be a
    normal filename, not a dotfile — `actions/upload-artifact` excludes
    hidden files by default) → `dispatch-sonar.yml` (validates the PR
    context artifact against strict patterns and dispatches `sonar.yml` via
    `actions/github-script`; no Sonar secret is available to this workflow,
    only `secrets.GITHUB_TOKEN`) →
    `sonar.yml` (re-verifies every dispatched value against the GitHub API,
    then gates on whether the verified head repo equals `github.repository`:
    for an external fork it stops there and posts a `neutral` GitHub Check
    instead of building, because SonarScanner for .NET — unlike the
    interpreted-language scanner action pyadcs uses — requires an actual
    `dotnet build` between `begin`/`end`, and MSBuild can execute
    fork-controlled logic (`Directory.Build.targets`/`.props`, NuGet
    build-time targets) in a job that has `SONAR_TOKEN`; for a same-repo PR
    it checks out the verified immutable head SHA, mints a scoped token via
    `actions/create-github-app-token` to post a GitHub Check, re-pins
    `SonarQube.Analysis.xml` from the base branch (so a PR cannot smuggle in
    a weakened exclusion list or a redirected `sonar.host.url` — strict, no
    fallback if the base ref lacks the file), then runs `dotnet sonarscanner
    begin/end` with the PR params sourced from the verified tuple, never
    from the raw dispatch inputs). The `pr.data.base.ref` validation in
    `sonar.yml` accepts exactly the branch set `check_pr.yml` triggers on
    (`main*`, `feat/*`, `hotfix/*`) — keep the two in sync if either changes.
  - **Push to `main`** — `sonar_push.yml` runs the scanner directly
    (`begin` → `dotnet build` → `dotnet test` with the same coverage
    collection → `end`, no `pullrequest.*` params) using the repo's own
    `SONAR_TOKEN`, since push events never run PR-authored code and there is
    no PR branch to re-pin `SonarQube.Analysis.xml` against. This maintains
    the SonarCloud main-branch baseline that PR analysis compares against.
  - Coverage is OpenCover format via the built-in `XPlat Code Coverage`
    collector (`coverlet.collector`); it activates automatically once a test
    project exists; today it produces no files and the placeholder file
    keeps the artifact upload from failing.
- **CodeQL** (`codeql.yml`) — Advanced-shape matrix over `actions` and
  `csharp`, both with `build-mode: none`, plus a weekly schedule.
- **Container security** — the shared `containers-test.yml` /
  `containers-build-and-push.yml` reusable workflows own the Trivy policy
  (org-default config); this repo no longer carries a local
  `config/trivy.yaml` override.

## Local-only paths

These may exist in a checkout but must never be committed:

- `.superpowers/` — already present in this checkout; self-excluded from
  git via a nested `.gitignore`.
- `docs/superpowers/` — same purpose, reserved for the same convention used
  in other OmniTrustILM repos (not currently present in this checkout).

Check `git status --ignored` before staging and make sure none of these are
included.
