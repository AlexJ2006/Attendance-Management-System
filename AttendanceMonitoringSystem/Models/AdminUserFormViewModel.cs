using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendanceMonitoringSystem.Models;

// UC06 - Create / update user accounts (wireframe 3.2.5, test T11).
// Data annotations give the "required field" and "incorrect format"
// messages (figures 3.2.5b and 3.2.5c). They are checked on the server,
// so they cannot be bypassed by turning off browser validation.
public class AdminUserFormViewModel
{
    // Null when creating a new account.
    public int? UserId { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name must be 50 characters or fewer.")]
    [RegularExpression(@"^[\p{L}][\p{L}' \-]*$",
        ErrorMessage = "First name can only contain letters, spaces, hyphens and apostrophes.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name must be 50 characters or fewer.")]
    [RegularExpression(@"^[\p{L}][\p{L}' \-]*$",
        ErrorMessage = "Last name can only contain letters, spaces, hyphens and apostrophes.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address, for example name@example.com.")]
    [StringLength(100)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(80)]
    public string Department { get; set; } = "";

    // At least one role must be chosen when creating an account.
    public List<int> SelectedRoleIds { get; set; } = new();

    [Display(Name = "Account active")]
    public bool IsActive { get; set; } = true;

    // Only used when creating an account.
    [DataType(DataType.Password)]
    [Display(Name = "Initial password")]
    public string? Password { get; set; }

    // Student details - required only when the Student role is selected.
    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    [Display(Name = "Cohort")]
    public int? CohortId { get; set; }

    [Range(1, 7, ErrorMessage = "Year of study must be between 1 and 7.")]
    [Display(Name = "Year of study")]
    public int? YearOfStudy { get; set; }

    // Drop-down options.
    public List<Role> AvailableRoles { get; set; } = new();
    public List<SelectListItem> Courses { get; set; } = new();
    public List<SelectListItem> Cohorts { get; set; } = new();

    public bool IsEdit => UserId.HasValue;
    public bool HasStudentRecord { get; set; }
}
