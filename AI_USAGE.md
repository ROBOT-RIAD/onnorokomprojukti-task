# AI USAGE REPORT

## 1. AI Tool Used
- **Tool**: Google Antigravity AI Coding Assistant (powered by Gemini).
- **Scope**: Used for project scaffolding, backend API development, frontend component architecture, automated test creation, and Docker orchestration configuration.

---

## 2. Parts Where AI Was Utilized
1. **Backend Development (.NET 8 Web API)**:
   - Scaffolding the ASP.NET Core project and configuring EF Core with PostgreSQL.
   - Designing the data models (`SchoolClass`, `SeatHold`) and API DTOs matching the exact contract in the specification.
   - Implementing `AdmissionService` with atomic transactions and row-level locking.
   - Creating `HoldCleanupBackgroundService` for automatic hold expiration maintenance.
   - Writing automated integration and concurrency tests in `backend.Tests`.

2. **Frontend Development (React 18/19 + Vite + TanStack Query)**:
   - Scaffolding raw JavaScript Vite React project without TypeScript.
   - Integrating TanStack Query for polling class seat counts (`refetchInterval: 3000`).
   - Implementing live countdown timer hook (`useCountdown`) and responsive UI with Bangla localization.
   - Writing Vitest and Testing Library unit tests.

3. **DevOps & Containerization**:
   - Creating multi-stage Dockerfiles for backend (.NET 8) and frontend (Node + Nginx).
   - Setting up `docker-compose.yml` with healthchecks, persistent volumes, and environment variable configuration.

---

## 3. Mistakes Made by AI & How They Were Detected and Fixed

### A. Vitest Matcher Incompatibility (`toBeDisabled`)
- **Mistake**: When generating unit tests in `frontend/src/App.test.jsx`, the AI generated assertions using `expect(button).toBeDisabled()`.
- **How Detected**: Running `npm test` failed with `Error: Invalid Chai property: toBeDisabled`.
- **Resolution**: Refactored the assertions to check standard DOM element properties directly: `expect(button.disabled).toBe(true)` and `expect(button.disabled).toBe(false)`, eliminating custom matcher dependencies and guaranteeing robust test runs.

### B. Cold Start Database Race Condition in Docker Compose
- **Mistake**: The AI initially called `db.Database.EnsureCreated()` directly at startup. When `docker compose up` starts both PostgreSQL and the backend simultaneously, PostgreSQL requires a few seconds to initialize its socket, leading to initial connection failures.
- **How Detected**: Identified during Docker Compose startup analysis.
- **Resolution**: Implemented a resilient 15-retry loop with a 2-second delay in `Program.cs` so that the backend waits gracefully until PostgreSQL is healthy before completing initialization.

### C. Multi-Process Concurrency Race Condition (Bonus Requirement)
- **Mistake**: The AI initially used an in-memory `SemaphoreSlim` in C#. While that protects a single process, it does not prevent race conditions if multiple backend processes run on different ports pointing to the same PostgreSQL database.
- **How Detected**: Reviewed against Section 1.6 Bonus criteria ("একই database ব্যবহার করে backend-এর দুইটা process আলাদা port-এ একসাথে চললেও নিয়ম ১ ঠিক থাকা").
- **Resolution**: Enforced PostgreSQL database-level row-level pessimistic locking (`SELECT "Id" FROM "Classes" WHERE "Id" = @classId FOR UPDATE`) inside a `ReadCommitted` transaction. This ensures that PostgreSQL itself serializes all seat allocations across any number of backend processes.

### D. Exact ISO 8601 Timestamp Formatting
- **Mistake**: Default .NET serialization outputs microsecond fractions (e.g. `2026-11-01T04:10:00.0000000Z`).
- **How Detected**: Cross-referenced with the exact contract sample: `"expiresAt": "2026-11-01T04:10:00Z"`.
- **Resolution**: Explicitly formatted UTC timestamps using `ToString("yyyy-MM-ddTHH:mm:ssZ")` in DTO mapping.
