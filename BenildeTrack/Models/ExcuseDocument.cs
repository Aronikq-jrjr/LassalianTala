using System.ComponentModel.DataAnnotations;

namespace BenildeTrack.Models;

public class ExcuseDocument
{
    public const string BenireType = "Benire Letter";

    public static readonly string[] Types = { "Excuse Letter", BenireType, "Medical Paper" };
    public static readonly string[] Statuses = { "Pending", "Accepted", "Rejected", "Acknowledged" };

    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    // The professor this document is addressed to (the one who reviews or signs it).
    [Required, Display(Name = "Professor")]
    public string ProfessorId { get; set; } = string.Empty;
    public ApplicationUser? Professor { get; set; }

    [Required, Display(Name = "Document Type")]
    public string DocumentType { get; set; } = Types[0];

    [DataType(DataType.Date), Display(Name = "Benire Start Date")]
    public DateTime? BenireStartDate { get; set; }

    [DataType(DataType.Date), Display(Name = "Benire End Date")]
    public DateTime? BenireEndDate { get; set; }

    // Stored (generated) file name on the server.
    [Required]
    public string FilePath { get; set; } = string.Empty;

    [Display(Name = "File Name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [Display(Name = "Date Submitted")]
    public DateTime DateSubmitted { get; set; } = DateTime.Now;

    [Required]
    public string Status { get; set; } = "Pending";

    [StringLength(500)]
    public string? Remarks { get; set; }

    // Set when a professor acknowledges (signs) a Benire letter.
    [Display(Name = "Acknowledged At")]
    public DateTime? SignedAt { get; set; }
}