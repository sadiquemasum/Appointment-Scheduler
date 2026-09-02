# Telenor Appointments Scheduling System

A single-page application that lets a Telenor service agent schedule, manage, and import customer appointments, with automatic conflict detection.

## Tech Stack

- **.NET 8** (ASP.NET Core Minimal API)
- **C# 12** (nullable reference types enabled, primary constructors, records)
- **SQLite** for local development, with a documented path to **PostgreSQL** for production (see "Database strategy" below)
- **EF Core 8** (Sqlite provider, swappable to Npgsql)
- **MediatR** for request/response handling (vertical slice architecture)
- **FluentValidation** for input validation
- **xUnit + Moq** for unit tests, **WebApplicationFactory** for integration tests
- Frontend: React + TypeScript *(in progress — see Frontend section)*

## Architecture

The backend follows a **vertical slice architecture**: each use case (Create, Update, Delete, List, Import, CheckConflict) is a self-contained folder under `src/Application/Appointments/`, containing its command/query, validator, and handler together, rather than being split across horizontal layers by technical concern.

**Why vertical slices over a traditional layered architecture:** the domain here is small (appointment CRUD + conflict detection + import), so a full n-tier Clean Architecture would add ceremony without adding clarity. Organizing by feature means each of the three assignment requirements maps directly to a folder a reviewer can open and read end-to-end.

**Result pattern:** handlers that can fail in an expected way (e.g. conflict detected) return a `Result`-style object (`CreateAppointmentResult`, `UpdateAppointmentResult`) rather than throwing exceptions, since a scheduling conflict is expected business behavior, not an exceptional failure. Exceptions are reserved for genuinely unexpected conditions.

## API Endpoints

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/appointments` | List appointments, optional `?from=&to=` date-range filter |
| `GET` | `/api/appointments/check-conflict` | Check if a proposed `start`/`end` (optional `excludeId`) conflicts with existing appointments, without creating anything |
| `POST` | `/api/appointments` | Create an appointment. `201` on success, `409` with conflict details on collision |
| `PUT` | `/api/appointments/{id}` | Update an appointment. `200`/`404`/`409`/`400` (route/body id mismatch) |
| `DELETE` | `/api/appointments/{id}` | Delete an appointment. `204`/`404` |
| `POST` | `/api/appointments/import` | Import appointments from the external calendar source (see below) |

Full interactive documentation is available via Swagger at `/swagger` when running locally.

## Assumptions

The assignment explicitly asks for documented assumptions where the spec is ambiguous. Here is every one made during development:

- **Conflict boundary — touching edges are not a conflict.** If one appointment ends at exactly the moment another begins (e.g. 10:00–10:30 followed by 10:30–11:00), this is *not* treated as a conflict. Two appointments only conflict if their time ranges genuinely overlap (`Start < other.End && other.Start < End`).
- **Customer representation.** A customer is represented as plain fields on the appointment itself (`CustomerName`, `CustomerPhone`, `CustomerEmail`) rather than a separate `Customer` entity/table. This was a deliberate simplification given the assignment scope — a real production system would likely have a proper customer aggregate.
- **Time zones.** All appointment times are stored and transmitted as `DateTimeOffset`, not `DateTime`, so the offset is always explicit and unambiguous. Clients (frontend or API consumers) are responsible for sending the correct offset for their local time zone; the backend does not assume a fixed time zone. **This was validated the hard way during testing**: a test using `TimeSpan.Zero` (UTC) instead of matching the mock external event's `+02:00` offset silently produced a *different real-world instant* despite identical wall-clock digits, and the conflict it was meant to test never triggered. This is exactly the kind of bug `DateTimeOffset` is designed to prevent at the type level, but it doesn't prevent a human from constructing the wrong offset by hand — worth calling out as a real lesson from building this.
- **Date-range filtering semantics.** `GET /api/appointments?from=&to=` returns any appointment that *overlaps* the given window at all (not just ones that start inside it) — consistent with how conflict detection itself works.
- **Appointment duration.** No fixed/default duration is enforced — the agent specifies both `start` and `end` explicitly for every appointment. `end` must be strictly after `start` (validated).
- **Update conflict re-check.** When updating an appointment's time, the conflict check excludes the appointment's own current record (via `excludeId`), so an appointment doesn't spuriously conflict with itself.
- **Import de-duplication.** External events are matched by an `ExternalId` field. Re-running an import is idempotent — already-imported events are skipped, not duplicated. Two external events that conflict with *each other* within the same import batch: the first is imported, the second is skipped as a conflict (first-imported-wins).
- **Import conflict handling.** If an external event's time conflicts with an existing appointment (or another event already imported earlier in the same batch), it is skipped, not force-imported. The import response reports counts of imported/skipped-duplicate/skipped-conflict plus human-readable conflict details, rather than failing the whole batch.
- **Route/body id consistency on update.** `PUT /api/appointments/{id}` requires the id in the URL to match the id in the request body; a mismatch returns `400 Bad Request` rather than silently preferring one over the other.
- **External calendar source.** Implemented against a simple mock endpoint (`GET /api/external/events`) built into this same API, standing in for the assignment's "simple test API" option. The import logic depends only on `IExternalCalendarClient`, an abstraction — swapping in a real provider (e.g. Google Calendar) requires only a new implementation of that interface and a one-line DI registration change; no changes to conflict-checking, deduplication, or the import handler itself.

## Database Strategy

**Local development uses SQLite** (`appointments.db`, via EF Core migrations) for zero-friction setup — no external services required to run and test the application.

**Production path: PostgreSQL via Docker Compose** *(planned, see note below)*. All EF Core queries were written to remain provider-agnostic (plain LINQ, no raw SQL, no SQLite-specific functions), so the swap from `UseSqlite` to `UseNpgsql` is a small, mechanical change. A Postgres-specific enhancement under consideration is an `EXCLUDE USING gist` constraint on the appointments table for database-level overlap prevention as defense-in-depth alongside the application-level `ConflictChecker` — this is intentionally optional/documented rather than a hard dependency, so the core logic doesn't rely on a Postgres-only feature.

*(This section will be updated once the Postgres + docker-compose.yml swap is completed.)*

## Planned, Not Implemented

The following are explicitly out of scope for this submission but are noted here since they came up during design discussion, per the assignment's guidance to document assumptions and reasoning:

- **Authentication/authorization.** No auth is implemented. In production, this would be JWT-based authentication scoped to service agents, per the role's stated security-mindset expectations.
- **Cloud deployment.** Not deployed. A production target would be AWS ECS Fargate (API) + RDS PostgreSQL (database), consistent with the role's cloud-native/DevOps focus.

## Testing

**56 automated tests** across two projects:

- **Unit tests (35)** — `Domain` logic (`TimeRange`, `ConflictChecker`, including the back-to-back boundary case and invalid-range guard), every command/query handler (with `IAppointmentRepository` mocked via Moq), and FluentValidation validator rules (required fields, max length, invalid email, end-before-start).
- **Integration tests (21)** — full HTTP round-trips via `WebApplicationFactory`, using a real (but in-memory) SQLite database that's freshly created per test run. Covers every endpoint's happy path plus its documented error paths (404, 409, 400), including import idempotency and conflict-skip behavior over real HTTP, and direct `AppointmentRepository` tests against a real `DbContext`.

Run all tests:
```bash
dotnet test
```

**Deliberately not covered**, as a conscious time trade-off rather than an oversight:
- Exhaustive input-format edge cases beyond what's listed above (e.g. every possible malformed date string variant)
- Load/performance testing
- Google Calendar API integration path (mock API was used instead — see Assumptions)

## Running Locally

Prerequisites: .NET 8 SDK, Node.js (for the frontend, once added).

```bash
# Restore and build
dotnet restore
dotnet build

# Apply database migrations (creates appointments.db)
dotnet ef database update --project src/Infrastructure --startup-project src/Api

# Run the API
cd src/Api
dotnet run
```

API available at `http://localhost:<port>` (port printed on startup). Swagger UI at `/swagger`.

## Repository Structure

See `TelenorAppointments.sln` for the full solution layout, or the Architecture section above.
