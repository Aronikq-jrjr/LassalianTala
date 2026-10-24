using System.ComponentModel.DataAnnotations;

namespace BenildeTrack.Models;

public class ExcuseUploadViewModel
{
    [Required, Display(Name = "Professor")]
    public string ProfessorId { get; set; } = string.Empty;

    [Required, Display(Name = "Document Type")]
    public string DocumentType { get; set; } = ExcuseDocument.Types[0];

    [DataType(DataType.Date), Display(Name = "Benire Start Date")]
    public DateTime? BenireStartDate { get; set; }

    [DataType(DataType.Date), Display(Name = "Benire End Date")]
    public DateTime? BenireEndDate { get; set; }

    [Display(Name = "File (PDF, JPG or PNG, max 5 MB)")]
    public IFormFile? File { get; set; }
}

public class ReviewViewModel
{
    public int Id { get; set; }

    [Required]
    public string Decision { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Remarks { get; set; }
}