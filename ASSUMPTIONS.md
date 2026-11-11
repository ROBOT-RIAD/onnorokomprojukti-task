# ASSUMPTIONS & DESIGN DECISIONS

This document records the architectural decisions, assumptions, and solutions implemented to satisfy all specifications and bonus objectives for the **School Admission System**.

---

## 1. Concurrency Control & Multi-Process Safety (Rule 1 & Bonus 1.6)

### Requirement:
Under no circumstance can `held + confirmed` exceed `totalSeats` for a class, even when thousands of requests arrive simultaneously, and even if **two backend processes run on different ports against the same database**.

### Solution:
- We rely on **PostgreSQL database-level row-level pessimistic locking (`SELECT ... FOR UPDATE`) within an explicit database transaction (`IsolationLevel.ReadCommitted`)**.
- When an application hold request arrives:
  1. The transaction begins and executes:
     ```sql
     SELECT "Id" FROM "Classes" WHERE "Id" = @classId FOR UPDATE;
     ```
  2. Any other concurrent transaction (whether on the same server, thread, or a different backend process on another port) attempting to hold a seat for that class is queued by PostgreSQL until the active transaction commits or rolls back.
  3. Inside the lock:
     - Active held seats are calculated as holds where `Status = 'HELD' AND ExpiresAt > UTC_NOW`.
     - Confirmed seats are calculated as holds where `Status = 'CONFIRMED'`.
     - If `held + confirmed >= totalSeats`, the transaction rolls back and immediately returns `409 Conflict`.
     - If student already has an active hold or confirmed admission in the class, it rolls back with `409 Conflict`.
     - Otherwise, the new hold is created and the transaction commits.
- This guarantees zero race condition and zero over-allocation across any number of clustered backend nodes or processes.

---

## 2. Dynamic Expiration & Automatic Seat Release (Rule 2)

### Requirement:
Hold duration is governed by `HOLD_DURATION_SECONDS` (default: 600). When a hold expires, its seat is automatically released without requiring any explicit API call.

### Solution:
- Available seat calculation is purely dynamic:
  $$\text{availableSeats} = \max(0, \text{totalSeats} - (\text{confirmedSeats} + \text{activeHeldSeats}))$$
  where `activeHeldSeats` only includes holds where `ExpiresAt > DateTime.UtcNow`.
- The moment `ExpiresAt` passes, `activeHeldSeats` decreases instantly without waiting for any API call or background job.
- In addition, an ASP.NET Core `BackgroundService` (`HoldCleanupBackgroundService`) runs periodically every 5 seconds to transition expired records in the database to status `'EXPIRED'`.
- Whenever `GET /holds/{holdId}` or `GET /me/holds` is requested, if a hold's status is still recorded as `HELD` but `ExpiresAt <= DateTime.UtcNow`, it is lazily updated and returned as `EXPIRED`.

---

## 3. Student Identification & Single Active Application (Rule 3 & Spec 1.3)

### Requirement:
No authentication/login required. Student identity is provided via a 17-digit birth registration number. A student cannot have more than one active hold or confirmed admission in the same class at the same time.

### Solution:
- The UI contains an identity input at the top of the header. It strictly validates the 17-digit numeric string (`^\d{17}$`) and persists it in `localStorage` (`school_admission_student_id`).
- When making API calls, this ID is forwarded via the `X-Student-Id` request header.
- If the header is missing, empty, or not 17 digits, the API returns a `400 Bad Request` with a helpful message.
- A student can apply to different classes/shifts, but cannot apply more than once to the same class/shift while having an active `HELD` or `CONFIRMED` status.

---

## 4. Idempotent Confirmation & Ownership (Rule 4 & Rule 5)

### Requirement:
Only the student's own non-expired `HELD` hold can be confirmed. Multiple confirmations on the same hold must not increase `confirmedSeats` multiple times.

### Solution:
- In `POST /holds/{holdId}/confirm`:
  1. The API verifies that the hold's `StudentId` matches the header `X-Student-Id`. If mismatched, it returns `403 Forbidden`.
  2. If the hold is already `CONFIRMED`, it idempotently returns `200 OK` with `{ "holdId": "...", "status": "CONFIRMED" }` without re-incrementing seats or erroring.
  3. If expired or cancelled, it returns `409 Conflict`.
- Payment simulation: Clicking "ভর্তি ফি দিন" assumes successful payment without requiring an external payment gateway.

---

## 5. UI Live Updates & State Resilience (UI Rules 9, 10, 11, 12)

- **Live Countdown & Expiry**: The `useCountdown` hook calculates the remaining seconds against `expiresAt`. When the timer hits 00:00, the UI immediately marks the hold status as `EXPIRED` and disables the "ভর্তি ফি দিন" payment button without requiring a page reload.
- **Seat Auto-Update**: TanStack Query polls `/classes` every 3 seconds (`refetchInterval: 3000`), keeping class seat counts live across all open browser tabs without manual refresh.
- **Persistence Across Browser Reopen**: Student ID and active hold ID are stored in `localStorage`. On page reload or opening a new browser tab, active applications and class states are automatically restored.
- **Error Handling**: Friendly Bangla & English alert banners communicate connection issues, duplicate applications, seat capacity exhaustion, and expiration clearly. No unhandled errors or blank screens.
