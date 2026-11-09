using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendanceMonitoringSystem.Models;

public class CreateSessionViewModel
{
    [Required(ErrorMessage = "Please select a module occurrence.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid module occurrence.")]
    [Display(Name = "Module")]
    public int ModuleOccurrenceId { get; set; }

    [Required(ErrorMessage = "Please enter a start date and time.")]
    [Display(Name = "Start date and time")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Please enter an end date and time.")]
    [Display(Name = "End date and time")]
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "Please select a session type.")]
    [StringLength(30)]
    [Display(Name = "Session type")]
    public string SessionType { get; set; } = "Lecture";

    public List<SelectListItem> ModuleOccurrences { get; set; } = new();
}
