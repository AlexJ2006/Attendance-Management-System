# Diagram consistency checklist

This is the single set of names, attributes and relationships that every diagram should use.
The code and `attendance.db` already follow it. If each diagram is updated to match, the
diagrams will also match each other.

---

## 1. Names to use everywhere

| Concept | Use this name | Avoid |
|---|---|---|
| The six roles / actors | **Student, Tutor, Module Leader, Course Leader, Academic Adviser, Administrator** | "Lecturer" (old database role, now renamed Tutor) |
| A scheduled class | **TeachingSession** (database table) / `Session` (C# class) | "Session" alone in the ERD |
| One delivery of a module | **ModuleOccurrence** | "Occurrence" |
| A student's mark for a session | **AttendanceRecord** (table) / `Attendance` (C# class) | |
| An audited change to a mark | **AttendanceCorrection** (table) / `AttendanceHistory` (C# class) | |
| The status values | **AttendanceStatus** enumeration | The enumeration box currently titled "AttendanceCorrection" in the class diagram |

> If you would rather the C# class names match the tables exactly (`TeachingSession`,
> `AttendanceRecord`, `AttendanceCorrection`), say so and the classes can be renamed. Otherwise,
> add a note to the class diagram such as "Session ↔ TeachingSession table".

---

## 2. Entities (ERD) and classes (class diagram)

There are **15 tables**. Each entity below should appear in the ERD with these attributes, and as
a class in the class diagram.

| Entity | Attributes (PK / FK marked) | Notes |
|---|---|---|
| **User** | PK UserId, Name, Email (unique), IsActive, PasswordHash, FailedLoginAttempts, LockoutEndUtc | **Remove** `corrected_at`, `reason` and `corrected_by` from User. They belong to AttendanceCorrection. |
| **Role** | PK RoleId, Name | 6 rows: the roles above. |
| **UserRole** | PK/FK UserId, PK/FK RoleId, AssignedAt | Lets one user hold several roles. |
| **Course** | PK CourseId, CourseName, CourseCode | |
| **Module** | PK ModuleId, FK CourseId, ModuleName, ModuleCode | |
| **ModuleOccurrence** | PK OccurrenceId, FK ModuleId, AcademicYear, Semester | |
| **TeachingSession** | PK SessionId, FK ModuleOccurrenceId, StartTime, EndTime, SessionType, Status (SessionStatus), SessionCode, CodeExpiresAt | **Add** SessionCode and CodeExpiresAt (the time-limited code, ADR02). |
| **Cohort** | PK CohortId, CohortName, AcademicYear | |
| **Student** | PK StudentId, FK UserId, FK CohortId, FK CourseId, FirstName, LastName, Email, YearOfStudy, Department | **Add** FK UserId, which links the student record to the sign-in account. |
| **AttendanceRecord** | PK AttendanceId, FK StudentId, FK SessionId, Status (**AttendanceStatus**), RecordedAt (**DateTime**), FK RecordedByUserId | **Fix**: Status is the enumeration, not Boolean; RecordedAt is DateTime, not String. Unique (StudentId, SessionId). |
| **AttendanceCorrection** | PK CorrectionId, FK AttendanceId, **PreviousStatus**, **NewStatus** (column `Status`), CorrectedAt, Reason, FK CorrectedBy → User | The class diagram already has previous and new status; add **PreviousStatus** to the ERD (T16). |
| **TutorAssignment** | PK TutorAssignmentId, FK TutorUserId → User, FK ModuleOccurrenceId | **Add to the ERD.** This replaces the "(lecturer)" link from TeachingSession to User in the class diagram. |
| **ModuleLeaderAssignment** | PK, FK ModuleLeaderUserId → User, FK ModuleId | **Add to the ERD and class diagram.** |
| **CourseLeaderAssignment** | PK, FK CourseLeaderUserId → User, FK CourseId | **Add to the ERD and class diagram.** |
| **AdviserAssignment** | PK, FK AdviserUserId → User, FK StudentId | **Add to the ERD and class diagram** (it is already in your database screenshot). |

**Remove `StudentGroup`** from the class diagram. It does not exist in the ERD or the code;
Cohort plays that role.

### Relationships (identical cardinality in the ERD and class diagram)

| From | To | Cardinality |
|---|---|---|
| User | UserRole | 1 — 0..* |
| Role | UserRole | 1 — 0..* |
| User | Student | 1 — 0..1 (a student account has one student record) |
| Course | Module | 1 — 1..* |
| Course | Student | 1 — 0..* |
| Cohort | Student | 1 — 0..* |
| Module | ModuleOccurrence | 1 — 1..* |
| ModuleOccurrence | TeachingSession | 1 — 0..* |
| Student | AttendanceRecord | 1 — 0..* |
| TeachingSession | AttendanceRecord | 1 — 0..* |
| User (recorder) | AttendanceRecord | 1 — 0..* |
| AttendanceRecord | AttendanceCorrection | 1 — 0..* |
| User (corrector) | AttendanceCorrection | 1 — 0..* |
| User (tutor) | TutorAssignment | 1 — 0..*; ModuleOccurrence 1 — 0..* |
| User (module leader) | ModuleLeaderAssignment | 1 — 0..*; Module 1 — 0..* |
| User (course leader) | CourseLeaderAssignment | 1 — 0..*; Course 1 — 0..* |
| User (adviser) | AdviserAssignment | 1 — 0..*; Student 1 — 0..1 |

### Enumerations (class diagram)

- **AttendanceStatus**: NotRecorded, Present, Absent, AuthorisedAbsence, LateArrival, LeftEarly
- **SessionStatus**: Scheduled, InProgress, Completed, Cancelled

### Methods in the class diagram

The code is layered (ADR01), so behaviour lives in **service classes**, not on the entities.
Either show attributes only on the entity classes, or move the methods to service classes:

| Method in your diagram | Where it really is |
|---|---|
| `AttendanceRecord.recordAttendance()` | `AttendanceService.RecordAttendance()` |
| `AttendanceRecord.updateStatus()` / `AttendanceCorrection.validateReason()` | `AttendanceService.CorrectAttendance()` (rejects an empty reason) |
| `getAttendancePercentage()` | `AttendanceCalculator.Percentage()` – one rule used everywhere |
| `TeachingSession.closeSession()` / `finaliseAttendance()` | `SessionStatusBackgroundService` (ADR07) |
| `User.canAccess(resource)` / `Role.hasPermission()` | `AccessScopeService.GetStaffScopeAsync()` and `[Authorize(Roles = …)]` |

---

## 3. C4 diagrams

**Level 1 and Level 2:** no changes, apart from using the role names in section 1 for the
person boxes. Level 2 should show one Web Application container (ASP.NET Core MVC) and one
SQLite Attendance Database, connected over HTTPS in production.

**Level 3 components and the code behind each:**

| C4 L3 component | Code |
|---|---|
| User Interface | `Views/`, `Pages/` (Razor) |
| Attendance Management | `Pages/Dashboard` (code entry), `TutorController`, `SessionsController`, `AttendanceService`, `CodeAttemptLimiter` |
| Monitoring and Reporting | `AttendanceReportsController`, `ReportService`, `CourseLeaderService`, `ModuleLeaderService`, `AttendanceCalculator` |
| User and Role Management | `AdminUsersController`, `TutorAssignments` / `ModuleLeaderAssignments` / `CourseLeaderAssignments` controllers, sign-in page, Account page |
| Academic Structure Management | Course / Module / ModuleOccurrence / TeachingSession models, `SessionsController.Create`, `ModuleService` |
| Access Control | Cookie authentication, default "must be signed in" policy (`Program.cs`), `[Authorize(Roles = …)]`, `AccessScopeService` |
| Data Access | `ApplicationDbContext` (EF Core), `DatabaseInitialiser` |
| **Add:** Scheduled Processing | `SessionStatusBackgroundService` (ADR07). Add this box; it reads and writes through Data Access. |

---

## 4. Use case diagram

- Actors: Student, Tutor, Module Leader, Course Leader, Academic Adviser, Administrator.
- Leaders and Advisers inherit "Generate attendance report" (UC05) from a general **Staff** actor.
- Use cases:
  - UC01 Sign in
  - UC02 Record attendance (Student)
  - UC03 View personal attendance (Student)
  - UC04 Manage session register (Tutor), with «extend» Correct attendance (reason required)
  - **Generate attendance code** (Tutor) – add this; UC02 depends on it
  - UC05 Generate attendance report (Staff), with «extend» Export CSV
  - UC06 Manage user accounts (Administrator), with «include» Assign roles

---

## 5. Sequence diagram (UC02) and flowchart (section 3.4.6)

The check order in the code is:

1. Is the student locked out after too many invalid codes? (T15) – **add this to both diagrams**
2. Is the code field empty? (T04)
3. Does the code match a session, and has it not expired? (T05)
4. Is the student on the course that owns the module? (eligibility)
5. Does an attendance record already exist? (T06)
6. Save the AttendanceRecord and show success.

Sequence participants, in order: **Student → Register Attendance page (StudentDashboardModel)
→ AccessScopeService → CodeAttemptLimiter → ApplicationDbContext → AttendanceService → SQLite**.

The attendance-viewing flowchart (3.4.7) already matches: the student is identified from the
sign-in claims through `AccessScopeService.GetCurrentStudentAsync()`.

---

## 6. Portfolio text to update

- **3.5.3 screenshots:** retake them. There are now **15 tables** and **2,088 records** in total.
- **3.6.1:** the "One-time-password" is the six-digit **password reset code**
  (Forgot password → code → Reset password), valid for 10 minutes and stored as a SHA-256 hash.
  Describe it that way. Authentication uses the **ASP.NET Core Identity password hasher with
  cookie authentication**, not the full Identity UI. Lockout is 15 minutes after 5 failed attempts.
- **STRIDE table:** Repudiation – change "Audit logs COULD record…" to say AttendanceCorrection
  records who, when, why and the previous and new status. Denial of Service – change "Rate
  limiting COULD…" to say invalid codes are limited to 5 per account per 10 minutes.
- **Wireframes 3.2.1:** sign-in uses **email**, not "UserID". Either update the wireframe or
  note the change.
- **Wireframes 3.2.4 / 3.2.5:** register edits happen on the session register page, and Create
  and Edit user are separate forms. **There is no Delete**: accounts are deactivated instead, so
  attendance history is kept. Worth one sentence of justification.
- **Appendix 1 (AI declaration):** your declaration says AI was used only to generate database
  data. Code and documentation have now been written with AI assistance as well, so update the
  declaration to match your university's policy before you submit.
