using AttendanceMonitoringSystem.Models;
using AttendanceMonitoringSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceMonitoringSystem.Controllers;

// UC05 - Generate attendance report (wireframe 3.2.3, tests T09 and T10).
// Every member of staff can run reports, but only on the modules and
// students inside their own academic scope (ADR05, test T08).
[Authorize(Roles = RoleNames.AllStaff)]
public class AttendanceReportsController : Controller
{
    private readonly AccessScopeService _scopeService;
    private readonly ReportService _reportService;
    private readonly ILogger<AttendanceReportsController> _logger;

    public AttendanceReportsController(
        AccessScopeService scopeService,
        ReportService reportService,
        ILogger<AttendanceReportsController> logger)
    {
        _scopeService = scopeService;
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ReportFilter filter, bool generate = false)
    {
        var scope = await _scopeService.GetStaffScopeAsync(User);
        var model = await BuildModelAsync(filter, scope);

        if (!generate)
        {
            return View(model);
        }

        var error = ReportService.Validate(filter);

        if (error != null)
        {
            model.ErrorMessage = error;
            return View(model);
        }

        // A module outside the user's scope is refused outright (T08).
        if (!model.Modules.Any(m => m.ModuleId == filter.ModuleId))
        {
            return Forbid();
        }

        if (filter.Student != ReportFilter.AllStudents &&
            !model.Students.Any(s => s.StudentId.ToString() == filter.Student))
        {
            return Forbid();
        }

        try
        {
            model.Report = await _reportService.GenerateAsync(filter, scope);
        }
        catch (Exception ex)
        {
            // Figure 3.2.3e - general report generation failure.
            _logger.LogError(ex, "Report generation failed.");
            model.ErrorMessage = "The report could not be generated. Please try again.";
            return View(model);
        }

        if (model.Report.Rows.Count == 0)
        {
            // Figure 3.2.3d - no matching records.
            model.InfoMessage = "No attendance records were found for the selected filters.";
            model.Report = null;
        }
        else
        {
            // Figure 3.2.3g - report generated successfully.
            model.SuccessMessage =
                $"Report generated successfully: {model.Report.Rows.Count} attendance " +
                $"record{(model.Report.Rows.Count == 1 ? "" : "s")} for " +
                $"{model.Report.Students.Count} student{(model.Report.Students.Count == 1 ? "" : "s")}.";
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(ReportFilter filter)
    {
        var scope = await _scopeService.GetStaffScopeAsync(User);
        var error = ReportService.Validate(filter);

        if (error != null)
        {
            TempData["ReportError"] = error;
            return RedirectToAction(nameof(Index), new { filter.ModuleId, filter.From, filter.To, filter.Student });
        }

        var modules = await _reportService.GetPermittedModulesAsync(scope);
        if (!modules.Any(m => m.ModuleId == filter.ModuleId))
        {
            return Forbid();
        }

        try
        {
            var report = await _reportService.GenerateAsync(filter, scope);

            if (report.Rows.Count == 0)
            {
                TempData["ReportInfo"] = "No attendance records were found for the selected filters, so there is nothing to export.";
                return RedirectToAction(nameof(Index), new { filter.ModuleId, filter.From, filter.To, filter.Student });
            }

            var module = modules.First(m => m.ModuleId == filter.ModuleId);
            var fileName =
                $"attendance-{module.ModuleCode}-{filter.From:yyyyMMdd}-{filter.To:yyyyMMdd}.csv";

            // Lets the page show "export complete" (figure 3.2.3h).
            Response.Cookies.Append("ams-export", "done", new CookieOptions
            {
                MaxAge = TimeSpan.FromMinutes(1),
                SameSite = SameSiteMode.Strict
            });

            return File(ReportService.ToCsv(report), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            // Figure 3.2.3f - CSV export failure.
            _logger.LogError(ex, "CSV export failed.");
            TempData["ReportError"] = "The report could not be exported to CSV. Please try again.";
            return RedirectToAction(nameof(Index), new { filter.ModuleId, filter.From, filter.To, filter.Student });
        }
    }

    private async Task<AttendanceReportPageViewModel> BuildModelAsync(ReportFilter filter, AccessScope scope)
    {
        return new AttendanceReportPageViewModel
        {
            Filter = filter,
            Modules = await _reportService.GetPermittedModulesAsync(scope),
            Students = await _reportService.GetPermittedStudentsAsync(scope, filter.ModuleId),
            ErrorMessage = TempData["ReportError"] as string,
            InfoMessage = TempData["ReportInfo"] as string
        };
    }
}

public class AttendanceReportPageViewModel
{
    public ReportFilter Filter { get; set; } = new();
    public List<Module> Modules { get; set; } = new();
    public List<Student> Students { get; set; } = new();
    public AttendanceReportResult? Report { get; set; }
    public string? ErrorMessage { get; set; }
    public string? InfoMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
