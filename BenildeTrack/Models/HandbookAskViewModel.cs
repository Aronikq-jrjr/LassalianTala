using System.ComponentModel.DataAnnotations;

namespace BenildeTrack.Models;

public class HandbookAskViewModel
{
    [Required, StringLength(500, MinimumLength = 5), Display(Name = "Your question")]
    public string Question { get; set; } = string.Empty;

    public string? Answer { get; set; }
    public string? Error { get; set; }
}