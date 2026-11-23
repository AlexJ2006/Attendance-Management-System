Diagram consistency checklist (condensed)

1. Names
Roles: Student, Tutor (not "Lecturer"), Module Leader, Course Leader, Academic Adviser, Administrator.
Tables vs. C# classes: TeachingSession / Session, ModuleOccurrence (not "Occurrence"), AttendanceRecord / Attendance, AttendanceCorrection / AttendanceHistory.
Rename the class-diagram enum box from "AttendanceCorrection" to AttendanceStatus.
Either rename the C# classes to match the tables, or add a mapping note to the class diagram.

2. ERD and class diagram (15 tables)

User: remove corrected_at, reason and corrected_by, which belong to AttendanceCorrection.
TeachingSession: add SessionCode and CodeExpiresAt.
Student: add FK UserId.
AttendanceRecord: Status should be the AttendanceStatus enum (not Boolean), and RecordedAt should be DateTime (not String). StudentId + SessionId must be unique.
AttendanceCorrection: add PreviousStatus to the ERD.
Add TutorAssignment to the ERD. It replaces the "(lecturer)" link in the class diagram.
Add ModuleLeaderAssignment, CourseLeaderAssignment and AdviserAssignment to both diagrams.
Remove StudentGroup. Cohort already does that job.
Make cardinalities match in both diagrams. Most are 1 — 0..*, with these exceptions:
User–Student: 1 — 0..1
Course–Module: 1 — 1..*
Module–ModuleOccurrence: 1 — 1..*
Student–AdviserAssignment: 1 — 0..1
Enums:
AttendanceStatus: NotRecorded, Present, Absent, AuthorisedAbsence, LateArrival, LeftEarly
SessionStatus: Scheduled, InProgress, Completed, Cancelled
Methods: take them off the entities, or move them to the service classes where they actually live (AttendanceService, AttendanceCalculator, SessionStatusBackgroundService, AccessScopeService).

3. C4

L1 and L2: only update the role names. L2 should show one ASP.NET Core MVC web app and one SQLite database, connected over HTTPS.
L3: add a Scheduled Processing component (SessionStatusBackgroundService) that uses Data Access. The other components already map to the code.

4. Use case diagram

Add a general Staff actor. Leaders and advisers inherit UC05 from it.
Add Generate attendance code for the Tutor, since UC02 depends on it.
Keep these relationships: Correct attendance «extend» UC04, Export CSV «extend» UC05, and Assign roles «include» UC06.

5. UC02 sequence diagram and flowchart

Check order:
Lockout check (add this to both diagrams)
Empty code
Code valid and not expired
Student eligible for the module
Duplicate record
Save
Sequence participants, in order: Student → StudentDashboardModel → AccessScopeService → CodeAttemptLimiter → ApplicationDbContext → AttendanceService → SQLite.

6. Portfolio text

3.5.3: retake the screenshots (15 tables, 2,088 records).
3.6.1: the "one-time password" is the 6-digit reset code. It's valid for 10 minutes and stored as a SHA-256 hash. Authentication uses the Identity password hasher with cookie auth, not the full Identity UI. Lockout is 15 minutes after 5 failed attempts.
STRIDE: change "COULD" to what's actually implemented:
Repudiation: the correction audit trail.
Denial of Service: invalid codes are limited to 5 per account per 10 minutes.
Wireframes:
Sign-in uses email, not UserID.
Register edits happen on the session page.
Create and Edit user are separate forms.
There's no Delete: accounts are deactivated instead, which keeps attendance history. Justify that in one sentence.
