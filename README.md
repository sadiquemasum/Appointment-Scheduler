# Appointments Scheduling System

A single-page application that lets a Telenor service agent schedule, manage, and import customer appointments, with automatic conflict detection.

## Tech Stack

**Backend**
- **.NET 8** (ASP.NET Core Minimal API)
- **C# 12** (nullable reference types enabled, primary constructors, records)
- **SQLite** for local and Docker development, with a documented path to **PostgreSQL** for production (see "Database Strategy" below)
- **Docker Compose** for containerized local runs (API + frontend; Postgres service defined but not yet enabled)
- **EF Core 8** (Sqlite provider, swappable to Npgsql)
- **MediatR** for request/response handling (vertical slice architecture)
- **FluentValidation** for input validation
- **xUnit + Moq** for unit tests, **WebApplicationFactory** for integration tests

**Frontend**
- **Vue 3 + TypeScript** (Composition API, `<script setup>`)
- **Vite** for tooling/dev server
- **TanStack Vue Query** for server state, caching, and mutations
- **VeeValidate + Zod** for form validation, mirroring the backend's validation rules
- **Axios** for HTTP
- **Vitest + Vue Testing Library** for component and schema tests
- **Prettier** for consistent formatting

## Architecture

The backend follows a **vertical slice architecture**: each use case (Create, Update, Delete, List, Import, CheckConflict) is a self-contained folder under `src/Application/Appointments/`, containing its command/query, validator, and handler together, rather than being split across horizontal layers by technical concern.

```
Appointment-Scheduler/
├── src/
│   ├── Domain/            # Appointment entity, TimeRange value object, ConflictChecker (no external deps)
│   ├── Application/       # MediatR commands/queries/handlers, validators, repository interfaces
│   ├── Infrastructure/    # EF Core DbContext, repository implementation, external calendar client
│   └── Api/               # Minimal API endpoints, Program.cs, DI wiring
├── tests/
│   ├── UnitTests/         # Domain logic, handler logic (mocked repository), validators
│   └── IntegrationTests/  # Full HTTP round-trips via WebApplicationFactory + in-memory SQLite
└── client/
    └── src/
        ├── api/           # Axios client and typed API call functions
        ├── components/    # AppointmentForm, AppointmentList, ImportButton
        ├── schemas/       # Zod validation schemas mirroring the backend's FluentValidation rules
        └── types/         # TypeScript types matching the API's response shapes
```

Dependencies in `src/` only ever point inward: `Api` depends on `Infrastructure`, which depends on `Application`, which depends on `Domain`. `Domain` itself has no dependencies.

**Why vertical slices over a traditional layered architecture:** the domain here is small (appointment CRUD + conflict detection + import), so a full n-tier Clean Architecture would add ceremony without adding clarity. Organizing by feature means each of the three assignment requirements maps directly to a folder a reviewer can open and read end-to-end.

### Request Flow

```
Vue 3 + TypeScript Client        External Calendar API (mock endpoint)
        |                                    |
        | HTTP / JSON                        | Import
        v                                    v
              Minimal API Endpoint
                       |
                       v
                FluentValidation
                       |
                       v
                 MediatR Handler
                       |
                       v
    Domain Logic (ConflictChecker, TimeRange)
                       |
                       v
       AppointmentRepository (EF Core)
                       |
                       v
         SQLite (Postgres planned)
                       |
                       v
    Response (success or error) back to Client
```

Each request enters through a Minimal API endpoint, passes FluentValidation, is dispatched via MediatR to its handler, which runs domain conflict-checking logic before persisting through EF Core. Conflict detection sits between the handler and persistence deliberately — it can reject a request before anything is written to the database. The frontend mirrors this with a live pre-submit conflict check (`GET /check-conflict`) as the agent picks a time, in addition to the hard server-side check on actual submission.

**Result pattern:** handlers that can fail in an expected way (e.g. conflict detected) return a `Result`-style object (`CreateAppointmentResult`, `UpdateAppointmentResult`) rather than throwing exceptions, since a scheduling conflict is expected business behavior, not an exceptional failure. Exceptions are reserved for genuinely unexpected conditions.

## API Endpoints

The API surface is organized into three categories, matching the three requirements the assignment asks for: **Core CRUD** for managing appointments directly, **Conflict Checking** as a standalone check the frontend can call before submitting, and **Import** for pulling appointments in from an external calendar source.

**Core CRUD**

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/appointments` | List appointments, optional `?from=&to=` date-range filter |
| `POST` | `/api/appointments` | Create an appointment. `201` on success, `409` with conflict details on collision |
| `PUT` | `/api/appointments/{id}` | Update an appointment. `200` / `404` / `409` / `400` (route/body id mismatch) |
| `DELETE` | `/api/appointments/{id}` | Delete an appointment. `204` / `404` |

**Conflict Checking**

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/appointments/check-conflict` | Check if a proposed `start`/`end` (optional `excludeId`) conflicts with existing appointments, without creating anything |

**Import**

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/api/appointments/import` | Import appointments from the external calendar source (see below) |

Full interactive documentation is available via Swagger at `/swagger` when running locally.

## Frontend Features

- **List view** with per-appointment detail (name, booked time, phone, email, notes), showing "—" for any empty optional field so every row has a consistent shape.
- **Date-range filter** wired to the backend's `from`/`to` query parameters, with an inclusive end-of-day boundary on the "To" date.
- **Create / Edit** via a single reusable form component, switching mode based on whether an appointment is passed in. Includes:
  - Client-side validation (VeeValidate + Zod) mirroring the backend's rules, as a fast first check — the backend remains the source of truth and validates again regardless.
  - A **live conflict check** as the agent fills in start/end times, calling `check-conflict` before submission so the warning appears before they click submit, not just after.
  - Explicit handling of `409` (conflict) and `404` (appointment deleted elsewhere) responses on submit.
- **Delete** with a native browser confirmation dialog before the request is sent.
- **Import** button showing a result summary (imported / skipped-duplicate / skipped-conflict counts, plus human-readable conflict details).
- Inputs are disabled while a create/update mutation is in flight, to prevent double-submission mid-save.

## Assumptions

The assignment explicitly asks for documented assumptions where the spec is ambiguous. Here is every one made during development:

- **Conflict boundary — touching edges are not a conflict.** If one appointment ends at exactly the moment another begins (e.g. 10:00–10:30 followed by 10:30–11:00), this is *not* treated as a conflict. Two appointments only conflict if their time ranges genuinely overlap (`Start < other.End && other.Start < End`).
- **Customer representation.** A customer is represented as plain fields on the appointment itself (`CustomerName`, `CustomerPhone`, `CustomerEmail`) rather than a separate `Customer` entity/table. This was a deliberate simplification given the assignment scope — a real production system would likely have a proper customer aggregate.
- **Time zones.** All appointment times are stored and transmitted as `DateTimeOffset`, not `DateTime`, so the offset is always explicit and unambiguous. Clients (frontend or API consumers) are responsible for sending the correct offset for their local time zone; the backend does not assume a fixed time zone. This was validated the hard way during testing: a test using `TimeSpan.Zero` (UTC) instead of matching the mock external event's `+02:00` offset silently produced a *different real-world instant* despite identical wall-clock digits, and the conflict it was meant to test never triggered. This is exactly the kind of bug `DateTimeOffset` is designed to prevent at the type level, but it doesn't prevent a human from constructing the wrong offset by hand.
- **Date-range filtering semantics.** `GET /api/appointments?from=&to=` returns any appointment that *overlaps* the given window at all (not just ones that start inside it) — consistent with how conflict detection itself works. The frontend's "To" date filter treats the selected day as inclusive (end-of-day, `23:59:59.999`), not its literal midnight start, so appointments later that same day aren't silently excluded.
- **Appointment duration.** No fixed/default duration is enforced — the agent specifies both `start` and `end` explicitly for every appointment. `end` must be strictly after `start` (validated on both client and server).
- **Update conflict re-check.** When updating an appointment's time, the conflict check excludes the appointment's own current record (via `excludeId`), so an appointment doesn't spuriously conflict with itself. The frontend passes the same `excludeId` through its live conflict check while editing.
- **Import de-duplication.** External events are matched by an `ExternalId` field. Re-running an import is idempotent — already-imported events are skipped, not duplicated. Two external events that conflict with *each other* within the same import batch: the first is imported, the second is skipped as a conflict (first-imported-wins).
- **Import conflict handling.** If an external event's time conflicts with an existing appointment (or another event already imported earlier in the same batch), it is skipped, not force-imported. The import response reports counts of imported / skipped-duplicate / skipped-conflict plus human-readable conflict details, rather than failing the whole batch.
- **Route/body id consistency on update.** `PUT /api/appointments/{id}` requires the id in the URL to match the id in the request body; a mismatch returns `400 Bad Request` rather than silently preferring one over the other.
- **External calendar source.** Implemented against a simple mock endpoint (`GET /api/external/events`) built into this same API, standing in for the assignment's "simple test API" option. The import logic depends only on `IExternalCalendarClient`, an abstraction — swapping in a real provider (e.g. Google Calendar) requires only a new implementation of that interface and a one-line DI registration change; no changes to conflict-checking, deduplication, or the import handler itself.
- **Frontend framework choice.** The job description this assignment was prepared for specifically names Vue3 as an example preferred framework, so Vue 3 + TypeScript was chosen over React/Angular (all three are explicitly permitted by the assignment) to match the target team's stack.

## Database Strategy

**Both local (non-Docker) development and the Docker Compose setup use SQLite** — for local dev this is `appointments.db` in `src/Api`, and inside the `api` container it's a file in the `api-data` named volume. Migrations are applied automatically on startup in both cases, so no manual database setup step is required.

**Production path: PostgreSQL** *(planned — not yet implemented as of this submission)*. All EF Core queries were written to remain provider-agnostic (plain LINQ, no raw SQL, no SQLite-specific functions), so the swap from `UseSqlite` to `UseNpgsql` is intended to be a small, mechanical change. The `docker-compose.yml` already has a Postgres service defined and ready — it's commented out pending the provider swap. A Postgres-specific enhancement under consideration is an `EXCLUDE USING gist` constraint on the appointments table for database-level overlap prevention as defense-in-depth alongside the application-level `ConflictChecker` — this would be an optional/documented enhancement rather than a hard dependency, so the core logic doesn't rely on a Postgres-only feature.

## Planned, Not Implemented

The following are explicitly out of scope for this submission but are noted here per the assignment's guidance to document assumptions and reasoning:

- **PostgreSQL provider swap.** See "Database Strategy" above — Docker Compose itself is implemented and working (API + frontend containers); only the Postgres provider and its compose service remain to be wired in.
- **Authentication/authorization.** No auth is implemented. In production, this would be JWT-based authentication scoped to service agents.
- **Cloud deployment.** Not deployed. A production target would be AWS ECS Fargate (API) + RDS PostgreSQL (database).
- **Google Calendar API integration.** A mock external API was used instead to demonstrate the import flow (see Assumptions); the abstraction is designed so a real provider can be substituted without touching business logic.

## Testing

**71 automated tests** across backend and frontend:

**Backend (58)**
- **Unit tests (37)** — `Domain` logic (`TimeRange`, `ConflictChecker`, including the back-to-back boundary case and invalid-range guard), every command/query handler (with `IAppointmentRepository` mocked via Moq), and FluentValidation validator rules (required fields, max length, invalid email, end-before-start).
- **Integration tests (21)** — full HTTP round-trips via `WebApplicationFactory`, using a real (but in-memory) SQLite database that's freshly created per test run. Covers every endpoint's happy path plus its documented error paths (404, 409, 400), including import idempotency and conflict-skip behavior over real HTTP, and direct `AppointmentRepository` tests against a real `DbContext`.

Run backend tests:
```bash
dotnet test
```

**Frontend (13)**
- **Schema tests (5)** — the Zod `createAppointmentSchema` (empty name, end-before-start, invalid email, valid data, empty optional fields).
- **Component tests (8)** — `AppointmentForm` (validation error display, live conflict warning, edit-mode pre-fill), `AppointmentList` (delete confirmation accepted/cancelled, date-filter query params), `ImportButton` (result summary on success, error message on failure). API calls are mocked so no real network requests happen in tests.

Run frontend tests:
```bash
cd client
npm test -- --run
```

**Deliberately not covered**, as a conscious time trade-off rather than an oversight:
- Exhaustive input-format edge cases beyond what's listed above (e.g. every possible malformed date string variant)
- Load/performance testing
- Google Calendar API integration path (mock API was used instead — see Assumptions)
- Frontend end-to-end (browser automation) tests — component tests plus the backend's comprehensive integration tests were judged to cover the important behavior without the added tooling overhead of a full E2E suite for this scope

## Running with Docker

Tested with **Docker 29.7.2** and **Docker Compose v5.5.0** (any reasonably recent Docker install with Compose v2+ should work).

From the project root:
```bash
docker compose up --build
```

This builds and starts two containers:
- **`api`** — the .NET 8 backend, published and run in a slim `aspnet:8.0` runtime image, listening on `http://localhost:8080`. Database migrations are applied automatically on startup, and the SQLite file persists in a named Docker volume (`api-data`) so data survives container restarts.
- **`client`** — the Vue 3 frontend, built with Vite and served via nginx, available at `http://localhost:8081`.

Open `http://localhost:8081` in your browser once both containers report as started — the frontend is pre-configured (via a build-time `VITE_API_BASE_URL`) to talk to the containerized API automatically.

To stop the containers (data persists in the volume for next time):
```bash
docker compose down
```

Swagger is not exposed by default in the container (it's gated behind the Development environment, same as any typical production setup) — the API itself is reachable directly for testing, e.g. `curl http://localhost:8080/api/appointments`.

**PostgreSQL is not yet wired into this Compose file** — see "Database Strategy" below. The service definition is present but commented out in `docker-compose.yml`, ready to enable once the Npgsql provider swap is done.

## Running Locally (without Docker)

Prerequisites: .NET 8 SDK, Node.js (LTS).

**Backend**
```bash
# Restore and build
dotnet restore
dotnet build

# Run the API - database migrations are applied automatically on startup
cd src/Api
dotnet run
```

(If you'd rather apply migrations explicitly before running, that's also still available: `dotnet ef database update --project src/Infrastructure --startup-project src/Api`.)

API available at `http://localhost:<port>` (port printed on startup). Swagger UI at `/swagger`.

**Frontend**
```bash
cd client
npm install
npm run dev
```

Frontend available at `http://localhost:5173`. Requires the backend to be running (CORS is configured for `localhost:5173`, `localhost:3000`, and the Docker frontend's `localhost:8081`).

Note: the local (non-Docker) run and the Docker run use separate SQLite database files, so data created in one won't appear in the other.