namespace BenildeTrack.Models;

public class HandbookOffense
{
    public int Id { get; set; }
    public string Section { get; set; } = string.Empty;   // e.g. "7.8.2.45"
    public string Severity { get; set; } = string.Empty;  // Minor or Major
    public string Category { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}