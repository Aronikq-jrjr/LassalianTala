using System.ComponentModel.DataAnnotations;

namespace BenildeTrack.Models;

public class Violation
{
    public static readonly string[] Statuses = { "Unresolved", "Resolved" };

    public int Id { get; set; }

    [Required, Display(Name = "Student")]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    [Required, StringLength(150)]
    public string Offense { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [DataType(DataType.Date), Display(Name = "Violation Date")]
    public DateTime ViolationDate { get; set; } = DateTime.Today;

    [Required, StringLength(20)]
    public string Status { get; set; } = "Unresolved";
}