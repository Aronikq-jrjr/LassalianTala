using System.ComponentModel.DataAnnotations;

namespace BenildeTrack.Models;

public class SchoolClass
{
    public int Id { get; set; }

    [Required, StringLength(20), Display(Name = "Course Code")]
    public string CourseCode { get; set; } = string.Empty;

    [Required, StringLength(100), Display(Name = "Course Name")]
    public string CourseName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Section { get; set; } = string.Empty;

    [Display(Name = "Professor")]
    public string? ProfessorId { get; set; }
    public ApplicationUser? Professor { get; set; }
}