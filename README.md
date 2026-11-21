# Attendance Monitoring System (AMS) – Proof of Concept

Software Architecture and Design (55-608809) – Alexander Jakeman (34005564)

A layered, modular-monolith ASP.NET Core MVC application (ADR01) using C#, Razor views,
Entity Framework Core and SQLite (ADR03), with Bootstrap for the user interface.

## Running the application

Requirements: the **.NET 10 SDK**.

```bash
cd AttendanceMonitoringSystem
dotnet run
```

Then open the URL shown in the terminal (for example `http://localhost:5xxx`).

On start-up, `Data/DatabaseInitialiser.cs` brings `attendance.db` up to date. It adds any
missing roles, columns and indexes, and it is safe to run every time.

## Demo accounts

All demo accounts use the password **`Password123!`**. Alex Morgan and Dr Casey Brown keep the
passwords that were already set on them.

| Role(s) | Name | Email | Scope |
|---|---|---|---|
<!-- | Administrator | IT Administrator | admin@example.com | Everything | -->
<!-- | Course Leader | Prof Mark Ellis | mark.ellis@example.com | Computer Science course; also Module Leader of CS101-M02 and M03 | -->
<!-- | Course Leader + Tutor | Dr Hannah Lowe | hannah.lowe@example.com | Business and Data Science courses; tutor for DS101 modules | -->
<!-- | Tutor + Module Leader | Dr Priya Sharma | priya.sharma@example.com | Tutor and Module Leader for BM101-M01 and M02 | -->
<!-- | Tutor + Module Leader | Dr Casey Brown | casey.brown@example.com | Tutor for CS101 modules; Module Leader of CS101-M01 | -->
<!-- | Academic Adviser | Sarah Okafor | sarah.okafor@example.com | Advisees on Business and Data Science | -->
<!-- | Student + Academic Adviser | Taylor Wilson | taylor.wilson@example.com | Advisees on Computer Science | -->
<!-- | Student (Computer Science) | Jamie Taylor | jamie.taylor@example.com | Own records only |
| Student (Business) | Sam Patel | sam.patel@example.com | Own records only |
| Student (deactivated) | Lucas Brooks | lucas.brooks25@example.com | Cannot sign in (T03) | -->

The demo data (staff, assignments and historical sessions from 28 Sep to 16 Nov 2026) is in
`Data/Seed/demo-data.sql`. It has already been applied to `attendance.db` and can safely be run
again.

### Demonstrating attendance registration (UC02)

1. Sign in as a Tutor, for example **priya.sharma@example.com**.
2. Go to **Staff → Create Session**, choose a module and set the session to start now.
3. Go to **Staff → Generate Attendance Code** and choose that session. A six-digit code is
   shown, valid for 10 minutes.
4. In another browser (or a private window), sign in as a student on that course, for example
   **sam.patel@example.com** for Business, and enter the code.

## How the portfolio maps to the code

| Portfolio item | Where it is implemented |
|---|---|
| UC01 User authentication (T01–T03) | `Pages/Index.cshtml(.cs)` – specific messages, lockout after 5 failed attempts, role-based landing page (`Services/RoleHome.cs`) |
| UC02 Record attendance (T04–T06, T15) | `Pages/Dashboard.cshtml(.cs)`, `Services/CodeAttemptLimiter.cs`, `Services/AttendanceService.cs` |
| UC03 View personal statistics (T07) | `Pages/MyAttendance.cshtml(.cs)` – overall %, % per module, monthly chart, history |
| UC04 Manage a session register (T16) | `Controllers/TutorController.cs`, `Controllers/SessionsController.cs` – reason required, audited in `AttendanceCorrection` with previous and new status |
| UC05 Generate report (T09, T10) | `Controllers/AttendanceReportController.cs`, `Services/ReportService.cs` – filters, preview and CSV export |
| UC06 Manage user accounts (T11) | `Controllers/AdminUsersController.cs`, `Views/AdminUsers/Form.cshtml` |
| NFR01 Availability | Friendly error pages (`Views/Shared/Error.cshtml`), `/health` endpoint with a database check, SQLite WAL mode |
| NFR02 Performance | Indexes created by `DatabaseInitialiser`; calculations done on request (ADR06); JMeter plan in `tests/jmeter` |
| NFR03 Role-aware access control (T07, T08) | Every page requires sign-in by default (`Program.cs`); `[Authorize(Roles=…)]`; `Services/AccessScopeService.cs` limits each user to their modules, courses or advisees (ADR05) |
| NFR04 Usability / WCAG 2.2 AA (T12–T14) | Labels on every field, skip link, visible focus, status shown with a symbol and text as well as colour (`Views/Shared/_StatusBadge.cshtml`) |
| ADR07 Scheduled processing | `Services/SessionStatusBackgroundService.cs` |
| Consistent calculation rules | `Services/AttendanceCalculator.cs` – Present, Late and Left Early count as attended; Authorised Absence is excluded |

## Tests

See [`tests/README.md`](tests/README.md). In summary:

- `tests/functional_tests.py` runs T01–T11, T15 and T16 automatically and prints a PASS/FAIL table.
- `tests/jmeter/ams-nfr02-peak.jmx` is the JMeter plan for NFR02.
- T12–T14 are manual (usability volunteers, keyboard and screen reader, Lighthouse, greyscale).

## Architecture documentation

[`docs/DIAGRAM-CHECKLIST.md`](docs/DIAGRAM-CHECKLIST.md) lists the names, attributes and
relationships the diagrams should use, so the ERD, class diagram, C4 diagrams and use-case and
sequence diagrams all match each other and the code.
