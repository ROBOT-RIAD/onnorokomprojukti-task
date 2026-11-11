# School Admission System (অনলাইন ভর্তি সিস্টেম)

A robust, full-stack, concurrent-safe online school admission system built for **Onnorokom Projukti Limited** recruitment assignment. The system operates on a **First-Come, First-Served (FCFS)** basis without lotteries, handling simultaneous peak-traffic applications with temporary seat holds, countdown expiration, and fee confirmation.

---

## 🛠️ Tech Stack

- **Backend**: C# (.NET 8 Web API)
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 8
- **Frontend**: React 19 + JavaScript (Raw JS, no TypeScript) + Vite + TanStack Query (React Query)
- **Testing**: xUnit, Microsoft.AspNetCore.Mvc.Testing, Vitest, Testing Library
- **Containerization**: Docker & Docker Compose

---

## 🚀 Quick Start with Docker (Recommended)

The entire application runs seamlessly with Docker. No external dependencies or software need to be installed on the host machine.

### 1. Run All Services
In the root directory of the repository, execute:

```bash
docker compose up --build
```

This single command starts:
1. **PostgreSQL Database** (`localhost:5432` with persistent Docker volume `postgres_data`)
2. **.NET 8 Backend API** on `http://localhost:8080`
3. **React Vite Frontend** on `http://localhost:3000`

### 2. Access the Application
- **Frontend Web UI**: [http://localhost:3000](http://localhost:3000)
- **Backend API Base URL**: [http://localhost:8080](http://localhost:8080)

### 3. Stop the Application
```bash
docker compose down
```
> **Data Persistence Guarantee**: When you run `docker compose down` and later `docker compose up`, all classes, holds, and student admissions are preserved in the persistent `postgres_data` volume.

---

## 🧪 Running Automated Tests

### 1. Backend Automated Tests (including Concurrency Test)
The backend test suite includes integration tests and a high-load concurrency test proving that seats are never over-allocated under concurrent requests.

To run the backend tests:
```bash
dotnet test
```

**Tests include:**
- `ClassApiTests`: Class creation, listing, seat counts calculation, validation rules.
- `HoldWorkflowTests`: End-to-end lifecycle (hold creation, student ownership check, single-active application rule, idempotent confirmation, cancellation, and student hold history).
- `ConcurrencyTests`: Sends 30 concurrent hold requests simultaneously to a class with only 5 seats; strictly asserts that exactly 5 succeed (201 Created), 25 receive conflict (409 Conflict), and `held + confirmed` never exceeds `totalSeats`.

### 2. Frontend Automated Tests
To run the React frontend tests:
```bash
cd frontend
npm test
```

---

## 📋 Features & Implementation Highlights

1. **Student Identification**:
   - No login required.
   - Students input their 17-digit Birth Registration Number at the top of the page.
   - Automatically saved to browser `localStorage` and sent with API requests in the `X-Student-Id` header.
   - Applications and active holds are automatically restored when reopening or refreshing the browser.

2. **Real-time Seat Updates without Page Reload**:
   - TanStack Query polls `GET /classes` every 3 seconds (`refetchInterval: 3000`).
   - Seat counts (Total, Held, Confirmed, Available) update live as other applicants apply.

3. **Live Expiration & Immediate UI Response**:
   - When a hold is created, a live timer counts down to expiration.
   - The moment the timer hits 00:00, the UI immediately marks the hold as `EXPIRED` and disables the "ভর্তি ফি দিন" payment button without requiring any page reload.
   - Expired seats are dynamically freed instantly on the backend.

4. **Multi-Process & Clustered Concurrency Safety (Bonus)**:
   - Uses PostgreSQL row-level pessimistic locking (`SELECT ... FOR UPDATE`) inside an explicit transaction.
   - Even if multiple backend instances run on separate ports against the same database, seat limit invariants are 100% strictly enforced.

5. **School Admin Panel**:
   - Easy creation of classes and shifts with preset shortcuts (e.g. "Class 6 – Morning Shift", "Class 6 – Day Shift").

---

## 🌐 Environment Variables Configuration

| Variable | Service | Default | Description |
| :--- | :--- | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | Backend | `Host=db;Port=5432;...` | PostgreSQL connection string |
| `HOLD_DURATION_SECONDS` | Backend | `600` | Duration (in seconds) for holding a seat |
| `VITE_API_URL` | Frontend | `http://localhost:8080` | Backend API URL for frontend client |

---

## 📡 API Contract Overview

All requests and responses use JSON. Student identity is transmitted via `X-Student-Id` header.

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/classes` | Create a new class/shift |
| `GET` | `/classes` | List all classes with seat statistics |
| `GET` | `/classes/{classId}` | Get single class seat statistics |
| `POST` | `/classes/{classId}/holds` | Apply for a seat hold (Header: `X-Student-Id`) |
| `GET` | `/holds/{holdId}` | Get current hold status (`HELD`, `CONFIRMED`, `EXPIRED`, `CANCELLED`) |
| `POST` | `/holds/{holdId}/confirm` | Confirm admission & pay fee (Header: `X-Student-Id`) |
| `DELETE` | `/holds/{holdId}` | Cancel application & release seat (Header: `X-Student-Id`) |
| `GET` | `/me/holds` | Get all applications of the student (Header: `X-Student-Id`) |

---

## 📂 Project Structure

```
├── backend/
│   ├── Controllers/
│   │   ├── ClassesController.cs
│   │   ├── HoldsController.cs
│   │   └── MeController.cs
│   ├── Data/
│   │   └── SchoolDbContext.cs
│   ├── DTOs/
│   ├── Exceptions/
│   ├── Middleware/
│   ├── Models/
│   ├── Services/
│   ├── Program.cs
│   └── Dockerfile
├── backend.Tests/
│   ├── ClassApiTests.cs
│   ├── ConcurrencyTests.cs
│   ├── CustomWebApplicationFactory.cs
│   └── HoldWorkflowTests.cs
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   │   ├── ClassList.jsx
│   │   │   ├── CreateClassForm.jsx
│   │   │   ├── HoldDetails.jsx
│   │   │   ├── MyApplications.jsx
│   │   │   ├── StudentHeader.jsx
│   │   │   └── useCountdown.js
│   │   ├── api.js
│   │   ├── App.jsx
│   │   ├── App.test.jsx
│   │   └── index.css
│   ├── nginx.conf
│   └── Dockerfile
├── docker-compose.yml
├── README.md
├── ASSUMPTIONS.md
└── AI_USAGE.md
```
