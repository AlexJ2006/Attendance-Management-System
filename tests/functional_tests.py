#!/usr/bin/env python3
"""
AMS functional test runner - portfolio section 3.7.1 / figure 4.6.

Runs the automatable functional tests (T01-T11, T15, T16) against a running
copy of the Attendance Monitoring System and prints a PASS/FAIL table you can
paste into the portfolio as evidence.

Usage (from the repository root):
    1. Copy the database so the tests do not change your demo data:
           cp AttendanceMonitoringSystem/attendance.db /tmp/ams-test.db
    2. Start the application against the copy:
           cd AttendanceMonitoringSystem
           ConnectionStrings__DefaultConnection="Data Source=/tmp/ams-test.db" dotnet run --urls http://localhost:5080
    3. In another terminal:
           python3 tests/functional_tests.py http://localhost:5080

Only the Python standard library is used. T12-T14 (usability, screen reader /
Lighthouse and greyscale checks) are manual - see tests/README.md.
"""
import datetime
from html import unescape
import http.cookiejar
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

BASE = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080").rstrip("/")
PASSWORD = "Password123!"

STUDENT = "sam.patel@example.com"             # Business Management student
OTHER_STUDENT = "jamie.taylor@example.com"    # Computer Science student
LOCKOUT_STUDENT = "noah.clarke9@example.com"  # used for T15
DEACTIVATED = "lucas.brooks25@example.com"
TUTOR = "priya.sharma@example.com"            # Tutor and Module Leader for BM101 modules
COURSE_LEADER = "mark.ellis@example.com"      # Course Leader for CS101 only
ADMIN = "admin@example.com"

results = []


class Client:
    def __init__(self):
        self.jar = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar))

    def request(self, path, data=None):
        body = urllib.parse.urlencode(data, doseq=True).encode() if data is not None else None
        try:
            with self.opener.open(BASE + path, body) as resp:
                return resp.status, resp.geturl(), resp.read().decode("utf-8", "replace")
        except urllib.error.HTTPError as e:
            return e.code, e.geturl(), e.read().decode("utf-8", "replace")

    def get(self, path):
        return self.request(path)

    def post_form(self, path, data, form_page=None):
        _, _, html = self.get(form_page or path)
        token = re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', html)
        if token:
            data = dict(data, __RequestVerificationToken=token.group(1))
        return self.request(path, data)

    def login(self, email, password=PASSWORD):
        return self.post_form("/Index", {"Email": email, "Password": password})


def alerts(html):
    found = re.findall(r'class="alert alert-(?:danger|warning|success)(?![^"]*d-none)[^"]*"[^>]*>(.*?)</div>', html, re.S)
    clean = [unescape(re.sub(r"<[^>]+>", " ", a)) for a in found]
    return " | ".join(" ".join(c.split()) for c in clean if c.strip())


def record(test, description, passed, evidence):
    results.append((test, description, "PASS" if passed else "FAIL", evidence))


def code_result(html):
    m = re.search(r'id="code-result"[^>]*>(.*?)</div>', html, re.S)
    return unescape(" ".join(re.sub(r"<[^>]+>", " ", m.group(1)).split())).lstrip("✓✗ ") if m else ""


def submit_code(client, code):
    _, _, html = client.post_form("/Dashboard?handler=SubmitCode", {"SessionCodeInput": code}, "/Dashboard")
    return code_result(html)


def main():
    # ---- T01 - valid login goes to the role's own area
    for email, expected in [(STUDENT, "/Dashboard"), (TUTOR, "/ModuleLeader"), (ADMIN, "/AdminDashboard")]:
        c = Client()
        status, url, _ = c.login(email)
        record("T01", f"Valid login ({email})", status == 200 and url.endswith(expected), url.replace(BASE, ""))

    # ---- T02 - missing or incorrect credentials
    c = Client()
    _, url, html = c.login(STUDENT, "wrong-password")
    record("T02", "Incorrect password rejected", "/Index" in url and "Invalid email or password" in html, alerts(html))
    _, url, html = c.login("", "")
    record("T02", "Empty fields rejected", "/Index" in url and "fill in all required fields" in html, alerts(html))

    # ---- T03 - deactivated account
    _, url, html = Client().login(DEACTIVATED)
    record("T03", "Deactivated account rejected", "deactivated" in html, alerts(html))

    # ---- Set-up: Tutor creates a live session and generates a code
    tutor = Client()
    tutor.login(TUTOR)
    now = datetime.datetime.now()
    tutor.post_form("/Sessions/Create", {
        "ModuleOccurrenceId": "4",
        "StartTime": (now - datetime.timedelta(minutes=5)).strftime("%Y-%m-%dT%H:%M"),
        "EndTime": (now + datetime.timedelta(hours=1)).strftime("%Y-%m-%dT%H:%M"),
        "SessionType": "Lecture"})
    _, _, html = tutor.get("/Sessions/GenerateCode")
    session_id = None
    for value, label in re.findall(r'<option value="(\d+)"[^>]*>\s*([^<]+)', html):
        if "BM101-M01" in label and now.strftime("%Y-%m-%d") in label:
            session_id = value
    _, _, html = tutor.post_form("/Sessions/GenerateCode", {"sessionId": session_id})
    m = re.search(r'generated-code-number">\s*(\d{6})', html)
    code = m.group(1) if m else None
    if not code:
        print("Could not create a live session/code - is the Tutor assigned to occurrence 4?")
        sys.exit(1)

    student = Client()
    student.login(STUDENT)

    # ---- T04 - empty code
    msg = submit_code(student, "")
    record("T04", "Empty attendance code", "enter an attendance code" in msg, msg)

    # ---- T05 - invalid / expired code
    msg = submit_code(student, "000000" if code != "000000" else "111111")
    record("T05", "Invalid or expired code", "invalid or has expired" in msg, msg)

    # ---- UC02 success
    msg = submit_code(student, code)
    record("UC02", "Valid code records attendance", "recorded successfully" in msg, msg)

    # ---- T06 - duplicate
    msg = submit_code(student, code)
    record("T06", "Duplicate submission prevented", "already been recorded" in msg, msg)

    # ---- Eligibility (section 3.4.6)
    other = Client()
    other.login(OTHER_STUDENT)
    msg = submit_code(other, code)
    record("UC02", "Student not on the module is refused", "not enrolled" in msg, msg)

    # ---- T07 - student cannot reach other students' data by changing the URL
    for path in ["/Students/EnrolmentDetails?studentId=2", "/Students", "/Attendance", "/Sessions/Register/4"]:
        status, url, _ = student.get(path)
        record("T07", f"Student requests {path}", status == 403, f"HTTP {status}")
    _, _, own = student.get("/MyAttendance")
    status, _, tampered = student.get("/MyAttendance?studentId=2")
    strip = lambda h: re.sub(r"\s+", "", re.sub(r'name="__RequestVerificationToken"[^>]*>', "", h))
    same = strip(own) == strip(tampered)
    record("T07", "MyAttendance ignores a studentId in the URL", status == 200 and same,
           "Same page as without the parameter" if same else "Page changed")

    # ---- T08 - staff outside their scope
    status, _, _ = tutor.get("/Sessions/Register/1")
    record("T08", "Business Tutor opens a Computer Science register", status == 403, f"HTTP {status}")
    status, _, _ = tutor.get("/Tutor?occurrenceId=1")
    record("T08", "Business Tutor selects a Computer Science module", status == 403, f"HTTP {status}")
    leader = Client()
    leader.login(COURSE_LEADER)
    status, _, _ = leader.get("/AttendanceReports?generate=true&ModuleId=6&From=2026-09-01&To=2026-12-31&Student=all")
    record("T08", "CS Course Leader reports on a Data Science module", status == 403, f"HTTP {status}")

    # ---- T09 - missing filters / invalid range
    _, _, html = leader.get("/AttendanceReports?generate=true&ModuleId=1")
    record("T09", "Report with missing filters", "complete all required filters" in html, alerts(html))
    _, _, html = leader.get("/AttendanceReports?generate=true&ModuleId=1&From=2026-11-30&To=2026-10-01&Student=all")
    record("T09", "Report with invalid date range", "Invalid date range" in html, alerts(html))

    # ---- T10 - valid report + CSV
    _, _, html = leader.get("/AttendanceReports?generate=true&ModuleId=1&From=2026-09-01&To=2026-12-31&Student=all")
    record("T10", "Report with valid filters", "Report generated successfully" in html, alerts(html))
    status, _, csv = leader.get("/AttendanceReports/ExportCsv?ModuleId=1&From=2026-09-01&To=2026-12-31&Student=all")
    rows = csv.strip().splitlines()
    record("T10", "CSV export", status == 200 and rows and rows[0].lstrip("﻿").startswith("Session Date"), f"{len(rows)} lines")

    # ---- T11 - invalid admin operations
    admin = Client()
    admin.login(ADMIN)
    _, url, html = admin.post_form("/AdminUsers/Create", {"FirstName": "", "Email": "not-an-email"}, "/AdminUsers/Create")
    record("T11", "Create user with missing/invalid details", "/AdminUsers/Create" in url and "required" in html, "Rejected with validation messages")
    _, url, html = admin.post_form("/AdminUsers/Create", {
        "FirstName": "Dup", "LastName": "Email", "Email": STUDENT, "Department": "Computing",
        "Password": "Password123!", "SelectedRoleIds": ["2"], "IsActive": "true"}, "/AdminUsers/Create")
    record("T11", "Create user with an existing email", "already exists" in html, "Rejected")
    _, _, html = admin.post_form("/AdminUsers/Activate", {"userId": "2"}, "/AdminUsers")
    record("T11", "Activate an already-active account", "already activated" in html, alerts(html))
    _, _, html = admin.post_form("/AdminUsers/Activate", {"userId": "999999"}, "/AdminUsers")
    record("T11", "Operation on an account that does not exist", "Account not found" in html, alerts(html))

    # ---- T15 - repeated invalid codes lock out one account only
    spammer = Client()
    spammer.login(LOCKOUT_STUDENT)
    for i in range(6):
        msg = submit_code(spammer, f"99999{i}")
    record("T15", "Account locked after repeated invalid codes", "Too many invalid" in msg, msg)

    # ---- T16 - correction needs a reason and is audited
    _, _, html = tutor.get(f"/Sessions/Register/{session_id}")
    student_ids = re.findall(r'name="studentId"\s+value="(\d+)"', html)
    target = student_ids[-1]
    tutor.post_form("/Sessions/RecordManualAttendance",
                    {"sessionId": session_id, "studentId": target, "status": "Absent"},
                    f"/Sessions/Register/{session_id}")
    _, _, html = tutor.post_form("/Sessions/RecordManualAttendance",
                                 {"sessionId": session_id, "studentId": target, "status": "Present", "reason": ""},
                                 f"/Sessions/Register/{session_id}")
    record("T16", "Correction without a reason is rejected", "provide a reason" in html, alerts(html))
    _, _, html = tutor.post_form("/Sessions/RecordManualAttendance",
                                 {"sessionId": session_id, "studentId": target, "status": "Present",
                                  "reason": "Arrived with a valid note"},
                                 f"/Sessions/Register/{session_id}")
    record("T16", "Correction with a reason is saved and audited", "correction was recorded" in html, alerts(html))

    # ---- Report
    width = max(len(r[1]) for r in results)
    print(f"\nAMS functional tests against {BASE}  ({datetime.datetime.now():%d/%m/%Y %H:%M})\n")
    print(f"{'Test':<5} {'Description':<{width}}  Result  Evidence")
    print("-" * (width + 40))
    for test, desc, outcome, evidence in results:
        print(f"{test:<5} {desc:<{width}}  {outcome:<6}  {evidence[:110]}")
    failed = [r for r in results if r[2] == "FAIL"]
    print(f"\n{len(results) - len(failed)}/{len(results)} checks passed.")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
