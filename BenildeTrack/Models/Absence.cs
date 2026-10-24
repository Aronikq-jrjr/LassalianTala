using System.ComponentModel.DataAnnotations;

namespace BenildeTrack.Models;

public class Absence
{
    public int Id { get; set; }

    [Required, Display(Name = "Student")]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    [Display(Name = "Class")]
    public int ClassId { get; set; }
    public SchoolClass? Class { get; set; }

    [DataType(DataType.Date), Display(Name = "Absence Date")]
    public DateTime AbsenceDate { get; set; } = DateTime.Today;

    [Display(Name = "Excuse Document")]
    public int? ExcuseDocumentId { get; set; }
    public ExcuseDocument? ExcuseDocument { get; set; }
}