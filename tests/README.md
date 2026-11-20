# AMS tests (portfolio section 3.7)

Run every test against a **copy** of the database so your demo data is not changed:

```bash
cp AttendanceMonitoringSystem/attendance.db /tmp/ams-test.db
cd AttendanceMonitoringSystem
ConnectionStrings__DefaultConnection="Data Source=/tmp/ams-test.db" dotnet run --urls http://localhost:5080
```

## Functional tests – T01–T11, T15, T16

In a second terminal, from the repository root:

```bash
python3 tests/functional_tests.py http://localhost:5080
```

The script uses only the Python standard library. It signs in as each role, creates a live
session, generates a code, and then checks each test case. It prints a table you can paste into
the portfolio. The expected result is **30/30 checks passed**.

The T15 check locks Noah Clarke out of code entry for 10 minutes. Restart the application to
clear this, because the limit is held in memory.

## Performance – NFR02 (JMeter)

The plan simulates the 9am peak:

- 100 student threads at about 52 requests per second: dashboard, code submission and My Attendance.
- 5 staff threads generating reports and exporting CSV.

```bash
cd tests/jmeter
jmeter -n -t ams-nfr02-peak.jmx -Jhost=localhost -Jport=5080 -l results.jtl -e -o report
```

Open `report/index.html`. For NFR02:

- Read the **95th percentile** column for the student requests; it should be under 2,000 ms.
- Read the column for "12 Generate attendance report"; it should be under 5,000 ms.

Optional properties: `-Jduration=120`, `-Jstudents=100`, `-JstudentRpm=2700` (about 45 requests
per second plus sign-ins), `-Jcode=123456` (submit a real live code instead of an invalid one).

Indicative result from development (60 s, 100 students and 3 staff, about 49 requests per second):

- 99.9% of ordinary requests completed under 2 s.
- The 95th percentile for the dashboard was 285 ms.
- 100% of reports completed under 5 s (95th percentile 38 ms).

Run it yourself on your own machine for the portfolio evidence.

## Manual tests – T12–T14

| Test | How |
|---|---|
| T12 Usability | Five volunteers (staff and students) sign in, register attendance with a code and find their attendance percentage without help. Record task success (target 80% or more) and a System Usability Scale questionnaire (target 68 or more). |
| T13 Accessibility | Chrome DevTools → Lighthouse → Accessibility on the sign-in, Register Attendance, My Attendance, Tutor dashboard, session register, Reports and Create User pages (target 90 or more). Repeat sign-in → register → view with keyboard only (Tab / Shift+Tab / Enter) and with VoiceOver (Cmd+F5). Error messages use `role="alert"` so they are announced. Indicative Lighthouse scores in development: 98–100 on every page tested. |
| T14 Greyscale | macOS: System Settings → Accessibility → Display → Colour filters → Greyscale. Every attendance status shows a symbol and words (✓ Present, ✗ Absent, A Authorised Absence, L Late Arrival, E Left Early, – Not Recorded), so none depends on colour. |
