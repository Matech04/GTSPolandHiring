# GTS Poland Hiring — Employee Profile Management API

A small CRUD service for managing employee profiles, plus a CSV bulk-import endpoint. Built with ASP.NET Core (.NET 10) and PostgreSQL.

## Running it locally

```bash
docker compose up --build
```

This starts a PostgreSQL container and the API container together. On startup the API automatically applies any pending EF Core migrations against the database, so there's no separate manual migration step — the schema is ready as soon as the container reports healthy. (The InMemory provider used by the test suite is explicitly excluded from that step, so it's a no-op there.)

- API: `http://localhost:8080`
- Interactive API docs (Scalar UI): `http://localhost:8080/scalar/v1`
- Postgres: `localhost:5432` (`postgres` / `Password123!`, database `GTSPolandHiringDb`)

Stop with `docker compose down` (add `-v` if you also want to drop the database volume and start fresh).

### Running the tests

```bash
dotnet test
```

No database or Docker needed — all 76 tests run against EF Core's InMemory provider.

## Technology choices

**.NET 10 / ASP.NET Core Minimal APIs** — strong typing and a mature ecosystem (EF Core, FluentValidation, MediatR) let me build a small CRUD service quickly without sacrificing structure. Minimal APIs avoid the controller boilerplate that would add nothing for a handful of endpoints this size.

**PostgreSQL** — a real relational database with proper unique constraints, which the "email must be unique" business rule genuinely needs as a backstop rather than just an application-level check. It's also trivial to run locally via Docker, which matters for the "include whatever is needed to run it locally" requirement.

**MediatR + FluentValidation (pipeline behavior)** — write operations (`POST /employee`, `PUT /employee/{id}`, `POST /employees/bulk`) go through a MediatR pipeline with a shared `ValidationBehavior`, so validation always runs before a handler executes and endpoints stay thin. Reads (`GET /employee/{id}`, `GET /employees`) and delete talk to `AppDbContext` directly from the endpoint — there's no validation or cross-cutting logic they need, so routing them through the same pipeline would just be ceremony.

**FluentResults** instead of exceptions for expected outcomes (not found, conflict, validation failure) — these are normal business outcomes, not exceptional ones, and `Result<T>` maps cleanly onto the `ProblemDetails`/HTTP-status mapping in `ResultExtensions`.

**CsvHelper** for bulk import — a mature, well-tested CSV parser that lets us catch malformed *rows* individually instead of failing the whole file on the first bad line.

**Vertical slice structure** (`Features/Employees/<Operation>/...`) — everything related to one operation (command, validator, handler, endpoint) lives together instead of being spread across generic `Controllers/Services/Repositories` layers. For a service this size it's easier to open one folder and see the whole story of a single use case.

## Data model

```
Employee
├─ Id              Guid (server-generated, time-ordered — see Design Decisions)
├─ Name            string
├─ HireDate        date
├─ Email           string (unique)
├─ PhoneNo         string
├─ ProfilePicture  string (URL)
├─ Status          enum (Active | Inactive)
├─ Address         string
├─ State           string
├─ Country         string
├─ City            string
├─ Pincode         string
└─ CreatedAt       date
```

## API

Base path: `/api/v1`

| Method | Path | Description |
|---|---|---|
| POST | `/employee` | Create a new employee |
| GET | `/employee/{id}` | Retrieve a single employee |
| GET | `/employees` | List all employees |
| PUT | `/employee/{id}` | Update an employee |
| DELETE | `/employee/{id}` | Delete an employee |
| POST | `/employees/bulk` | Bulk import employees from a CSV file |

### Create an employee

```bash
curl -X POST http://localhost:8080/api/v1/employee \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Anna Nowak",
    "hireDate": "2024-01-01",
    "email": "anna.nowak@company.com",
    "phoneNo": "+48123456789",
    "profilePicture": "https://example.com/a.png",
    "status": "Active",
    "address": "Main Street 1",
    "state": "Mazowieckie",
    "country": "Poland",
    "city": "Warsaw",
    "pincode": "00-001"
  }'
```

Returns `200 OK` with the new employee's `id` as the body. *(A pure REST purist would expect `201 Created` with a `Location` header here — see "What I'd do differently".)*

### Get / list

```bash
curl http://localhost:8080/api/v1/employee/{id}
curl http://localhost:8080/api/v1/employees
```

`GET /employee/{id}` returns `404` with a `ProblemDetails` body (`errorCode: EMPLOYEE_NOT_FOUND`) if the id doesn't exist.

### Update

```bash
curl -X PUT http://localhost:8080/api/v1/employee/{id} \
  -H "Content-Type: application/json" \
  -d '{ ... same shape as create ... }'
```

`404` if the employee doesn't exist, `409` (`EMPLOYEE_EMAIL_ALREADY_EXISTS`) if the new email belongs to a different employee.

### Delete

```bash
curl -X DELETE http://localhost:8080/api/v1/employee/{id}
```

`204 No Content` on success, `404` if the id doesn't exist.

### Bulk import

```bash
curl -X POST http://localhost:8080/api/v1/employees/bulk \
  -F "file=@employees_sample.csv;type=text/csv"
```

Expected CSV header: `Name,Hiredate,Email,PhoneNo,ProfilePicture,Status,Address,State,Country,City,Pincode`.

Response body:

```json
{
  "totalRows": 10,
  "imported": 6,
  "skipped": 0,
  "failed": 4,
  "issues": [
    { "row": 3, "email": "sarah.johnson@company.com", "status": "Failed", "errors": [{ "code": "EMPLOYEE_HIREDATE_CANNOT_BE_FUTURE", "message": "..." }] }
  ]
}
```

The import never fails the whole request just because some rows are bad — see "Bulk import failure handling" below.

### Error shape

All failures (validation, not-found, conflict, unsupported file, etc.) come back as `ProblemDetails` with an `errorCode` extension field, e.g.:

```json
{
  "status": 409,
  "title": "Conflict",
  "detail": "Employee with email 'anna.nowak@company.com' already exists.",
  "errorCode": "EMPLOYEE_EMAIL_ALREADY_EXISTS"
}
```

Status codes used: `400` (validation), `404` (not found), `409` (conflict), `413` (file too large), `415` (unsupported file type), `500` (unexpected server error).

## Business rules

The assignment requires the three below plus 3–4 more of our own choosing.

**Required:**
1. **Email is unique across all employees** — enforced both by a `UNIQUE` index in Postgres (the real guard) and an application-level pre-check (for a clean `409` instead of a raw database error in the common case).
2. **HireDate cannot be in the future** — checked against `DateTime.UtcNow` at request time.
3. **Phone number must be a valid format** — accepts E.164 optionally decorated with spaces, hyphens, or parentheses (e.g. `+48 123 456 789`, `(123) 456-7890`), since real-world phone numbers are rarely typed in strict E.164.

**Added:**
4. **Email must belong to the configured company domain** (`CompanyPolicy:AllowedEmailDomain`, default `company.com`) — this is an internal HR system, so a personal Gmail/Yahoo address is almost certainly a data-entry mistake, not a legitimate employee email.
5. **HireDate cannot be earlier than the company's founding date** (`CompanyPolicy:CompanyFoundedDate`) — an employee cannot have been hired before the company existed; catches obviously wrong dates that "not in the future" alone wouldn't.
6. **ProfilePicture, if provided, must use HTTPS** — it's stored as a URL and would typically be rendered directly in a UI `<img>` tag, so an `http://` (or non-URL) value is rejected rather than silently accepted.
7. **Status must be a valid enum name, rejected outright if not** — this is a direct response to the assignment's own callout ("invalid status silently corrected instead of rejected"): an unrecognized status is a `400`, never silently coerced to a default.

All string fields also have a `MaximumLength` validation rule that mirrors the database column's length exactly, so an over-long value is rejected with a clean `400` instead of surfacing as an unhandled database error.

## Bulk import failure handling

This was the part of the assignment I spent the most time reasoning about, since "how do you handle failures in bulk import" is called out explicitly in the interview follow-up.

- **Per-row validation, not all-or-nothing.** Each CSV row is parsed and validated independently. A malformed row (bad CSV syntax) or a row that fails business validation is recorded as a `Failed` issue with the specific error code(s) and processing continues — one bad row never aborts the whole file.
- **Duplicate detection is case-insensitive** and checks both against rows already in the database and against other rows earlier in the same file, so `Jane@Company.com` and `jane@company.com` are correctly treated as the same address (the value is still *stored* with whatever casing was submitted).
- **New rows are saved in one batch when possible, with a per-row fallback if that fails.** The common case is a single `SaveChangesAsync` call for every valid row in the file. If that throws (e.g. another request inserted a colliding email in the moment between our own pre-check and this save), nothing from the batch was persisted — `SaveChangesAsync` is one transaction — so the handler falls back to saving rows one at a time, and only the row that genuinely still conflicts is reported as `Skipped`; every other row in the file is still imported instead of the whole request failing with a `500`.
- **A known, deliberately accepted gap:** the same "pre-check then save" race exists in `POST /employee` and `PUT /employee/{id}`, but I decided *not* to add the same recovery logic there. The database's unique index still prevents bad data either way — the only difference is that a genuine race there would surface as an unhandled `500` instead of a clean `409`, in a window that's already extremely narrow (two requests for the exact same new email, at the exact same moment). Building generic infrastructure to close that gap felt disproportionate to a take-home assignment's scope; I judged it worth the engineering effort for bulk import specifically because that's the feature Part 2 is explicitly about, not for the simple CRUD paths.

## Testing strategy

- **Validator tests** (`*ValidatorTests`) using `FluentValidation.TestHelper` — one test per rule, including the boundary cases (`HireDate` exactly on the company's founding date, phone numbers with common separators, etc.).
- **Handler tests** using EF Core's InMemory provider — verify the actual persistence outcome (what got saved, what didn't), not just the returned `Result`.
- **Endpoint tests** using `WebApplicationFactory` — exercise the full pipeline (model binding → validation → handler → HTTP response) for at least one endpoint per feature, including status codes and `ProblemDetails` shape.
- **Bulk import edge cases**: malformed CSV rows, an unterminated quote, an oversized field, duplicate emails (within the file and against the database, including case-only differences) — each with a regression test.
- **A genuinely interesting discovery while testing the race-condition recovery path**: EF Core's InMemory provider does *not* enforce unique indexes at all (verified directly — two entities with the same "unique" email save without complaint). That means no InMemory-only test could ever exercise a real duplicate-key failure. I worked around this with a small `SaveChangesInterceptor` test helper that injects a "racing" row and throws exactly as Postgres would on a real constraint violation — deterministic and fast, without needing a real database in the test suite. Worth knowing: this also means the test suite alone can't prove the *actual* Postgres unique constraint behaves as expected — that would need an integration test against a real database (see "What I'd do differently").

## What I'd do differently with more time

- Return `201 Created` with a `Location` header from `POST /employee` instead of `200 OK`.
- Add authentication/authorization — every endpoint is currently open, which is fine for this exercise but not for a service storing employee PII.
- Add pagination to `GET /employees` — it currently returns the entire table.
- Apply the same case-insensitive email comparison used in bulk import consistently to `POST /employee` and `PUT /employee/{id}` (today only bulk import treats casing as insignificant).
- Add an integration test against a real PostgreSQL instance (e.g. via Testcontainers) specifically to verify the unique constraint's actual behavior, since the InMemory provider can't be trusted for that (see Testing strategy above).

## Design decisions

- **Vertical slices over layered architecture** — each operation's command, validator, handler, and endpoint live in one folder, so the whole story of a use case is in one place instead of spread across generic layers.
- **`Guid.CreateVersion7()` for primary keys** — time-ordered GUIDs avoid the "unsafe ID generation" pitfall of guessable sequential integers while still being reasonably index-friendly, unlike fully random v4 GUIDs.
- **Business-rule constants as bound, validated configuration** (`CompanyPolicy:AllowedEmailDomain`, `CompanyPolicy:CompanyFoundedDate`, `BulkImport:MaxFileSizeBytes`) with `ValidateOnStart()` — keeps them environment-configurable instead of hardcoded, and fails fast at startup rather than on the first request if misconfigured.
- **Auto-applying EF Core migrations on startup**, guarded by `Database.IsRelational()` — makes `docker compose up` fully self-sufficient for local evaluation with no separate migration step, while remaining a safe no-op against the InMemory provider used by the test suite.
- **A shared `EmployeeInputValidator<T>` base class** used by create, update, and each bulk-import row — the three operations validate the same employee fields, so the rules (and their error codes) are defined exactly once.

## AI tool usage

Architecture and project structure decisions (vertical slices, the MediatR/FluentValidation pipeline, the `Result`-based error handling) came from my own experience, sanity-checked through a design discussion with **Gemini** rather than generated by it. API versioning (`/api/v{version}`) was my own proposal, not something either AI suggested.

**Claude Code** did most of the repetitive, boilerplate implementation work — the near-identical command/validator/endpoint scaffolding repeated across the five CRUD operations — and wrote the majority of the test suite. Delegating test-writing specifically freed up enough time that I could afford much broader coverage (validators, handlers, endpoints, and a set of bulk-import edge cases) than the assignment's time box would otherwise have allowed.

A few things I corrected from its initial suggestions:
- **`Employee.Id` generation** — it initially generated the id on the database side; I changed it to generate a time-ordered `Guid.CreateVersion7()` value in application code instead (with EF's `ValueGeneratedNever()`), which avoids depending on the database's default UUID behavior and produces ids that are neither sequential/guessable nor purely random — directly relevant to the "unsafe ID generation" pitfall the assignment calls out.
- **`Id` property setter** — it used a mutable `set`; I changed it to `init`, since an identity value should never be reassignable after the entity is constructed.

In addition, Claude Code was used for a structured review pass after the initial implementation was working, specifically to audit it against this assignment's brief. That pass found and fixed several concrete issues:

- Field-length validation didn't mirror the database column limits, so an over-long value would pass validation and only fail as an unhandled `500` on save — fixed by adding matching `MaximumLength` rules.
- A single bad or racing row in a bulk import could abort the entire batch with a `500`, discarding the report for every otherwise-valid row — fixed with the batch-then-per-row-fallback save strategy described above.
- `docker-compose.yaml` had a password mismatch between the database and API containers, and the API never applied migrations on startup — both fixed.


