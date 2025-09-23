# Tasker

Tasker is an offline-first task and project manager for the terminal. It combines a Spectre.Console UI with secure accounts, time tracking, and optional cloud sync backed by PostgreSQL. Local work is always persisted to SQLite so you can stay productive even without a network connection.

## Features
- Task management with rich table/detail views, filtering, status updates, and completion workflow.
- Project dashboard with aggregated time estimates/actuals, search by name, and direct task management shortcuts.
- Built-in time tracking: active tasks accrue minutes automatically; pause/blocked/testing states preserve history.
- Secure user accounts using BCrypt password hashing, lockout after repeated failures, and encrypted session tokens bound to the current machine.
- Offline-first persistence backed by SQLite with background synchronization to PostgreSQL when the server becomes available.
- Interactive setup and settings screens to configure database connectivity, auto-login, and session durations.
- Pluggable architecture (Domain/Core/Infrastructure/Cli) that keeps business logic, persistence, and presentation layers decoupled.

## Architecture
- `Domain/` – Entity models (`User`, `Tasks`, `Project`, etc.) plus domain-level encryption helpers.
- `Core/` – Interfaces for repositories and services that define the application contract.
- `Infrastructure/` – EF Core contexts for SQLite and PostgreSQL, database manager, repositories, sync engine, and migrations for both providers.
- `Cli/` – Spectre.Console-powered interactive UI, services that implement business use cases, setup/login flows, and local configuration helpers.
- `Tests/` – xUnit test project scaffold ready for unit and integration coverage.

## Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/) (9.0.1 or newer).
- SQLite (bundled with .NET, no extra install required).
- Optional: PostgreSQL 13+ for cloud synchronization.
- Optional: `dotnet-ef` CLI when adding or evolving EF Core migrations.

## Getting Started
1. Clone the repository and change into the solution directory:
   ```bash
   git clone https://github.com/<your-org>/Tasker.git
   cd Tasker
   ```
2. Restore and build the solution:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Run the CLI (the first run walks through initial setup):
   ```bash
   dotnet run --project Cli
   ```

### First-time Setup
- **PostgreSQL mode:** Provide a standard connection string (e.g. `Host=localhost;Port=5432;Database=tasker;Username=postgres;Password=secret`). The value is encrypted per-machine and stored in the user config directory.
- **Local-only mode:** Choose "Run locally only (SQLite only)" to keep everything on disk. You can add a server connection later from `Settings → Database Settings`.

On startup the app applies pending EF Core migrations automatically for both SQLite and PostgreSQL.

## Running the CLI
After setup, launch Tasker with `dotnet run --project Cli`. Successful logins create a session token that is saved (encrypted) for auto-login. Existing sessions can be toggled or reconfigured from the Settings menu.

To produce a distributable binary:
```bash
dotnet publish Cli/Cli.csproj -c Release -r linux-x64 --self-contained false
```
Replace the RID with the target platform (`win-x64`, `osx-arm64`, etc.). Published binaries land under `Cli/bin/Release/net9.0/<rid>/publish/`.

## Application Data & Storage
Tasker keeps user data in the OS application data folder:

| Platform | Location |
| --- | --- |
| Windows | `%APPDATA%\\Tasker` |
| macOS / Linux | `~/.config/Tasker` |

You will find:
- `tasker_local.db` – the local SQLite database that holds all entities.
- `config.json` – encrypted connection string and session token metadata.

Connection strings and session tokens are encrypted using machine-specific keys, so copies moved to a different host cannot be decrypted.

## Database Synchronization
The `SyncService` provides bi-directional sync between SQLite and PostgreSQL:

- Works transparently in the background once you log in.
- Detects connectivity using `ConnectionMonitor`; syncs when the server comes online.
- Uploads users, projects, tasks, and sessions in dependency order, marking entities as synced.
- Handles server-side updates and pulls them into SQLite.
- Shields you from conflicts by prompting for username changes if a clash is detected and by regenerating GUIDs when necessary.

If PostgreSQL is unavailable, the app keeps working locally and tries again later.

## CLI Walkthrough
- **Tasks**
  - List tasks with priority, status, estimates, and live tracked time.
  - View full details, filter by status, add new tasks, update existing ones, mark complete, or delete.
  - Per-project task creation prevents duplicate titles within the same project/user.
- **Projects**
  - View project summaries with cumulative time math, inspect details, search by name, or manage project-specific tasks.
  - Create, update, or delete projects; descriptions can be edited in an external editor via the `TextEditor` helper.
- **Settings**
  - *Login Settings:* Toggle auto-login or change session duration for the current machine.
  - *Database Settings:* Inspect or replace the PostgreSQL connection string (stored encrypted) or revert to local-only mode.

## Security Highlights
- User passwords hashed with BCrypt (cost 12) and zeroed in memory after use.
- Accounts lock for 15 minutes after five failed login attempts.
- Session tokens are random 32-byte values bound to the machine ID, encrypted before being written to disk, and validated on use.
- Connection strings are encrypted with PBKDF2-derived keys unique to the host.
- Domain models expose `Encrypt`/`Decrypt` helpers so sensitive fields can be protected before leaving the user’s device.

## Project Layout
```
Tasker/
├── Cli/              # Interactive console app and UI flows
├── Core/             # Service and repository interfaces
├── Domain/           # Entity models and domain helpers
├── Infrastructure/   # EF Core persistence, sync, migrations
├── Tests/            # xUnit test project scaffold
├── tasker/           # Example published binary artifact
└── Tasker.sln        # Solution file
```

## Testing
Run all tests with:
```bash
dotnet test
```
The current test project is a placeholder—extend it with unit tests for services, repositories, and synchronization behavior as the project evolves.

## Working with Migrations
Create a new migration targeting both providers from the solution root:
```bash
dotnet ef migrations add <MigrationName> \
  --project Infrastructure \
  --startup-project Cli
```
Update the databases manually if needed with:
```bash
dotnet ef database update --project Infrastructure --startup-project Cli
```

## Troubleshooting
- **Cannot connect to PostgreSQL:** Re-open `Settings → Database Settings`, test, and save a valid connection string. Ensure firewalls permit access.
- **Auto-login fails after moving machines:** Session tokens and connection strings are machine-bound; re-authenticate on the new host.
- **EF tooling errors:** Confirm the .NET 9 SDK is the active version (`dotnet --info`) and reinstall `dotnet-ef` if necessary.
