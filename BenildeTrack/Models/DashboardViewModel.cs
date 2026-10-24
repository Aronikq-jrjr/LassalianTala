namespace BenildeTrack.Models;

public class DashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Student
    public int TotalViolations { get; set; }
    public int UnresolvedViolations { get; set; }
    public int TotalAbsences { get; set; }
    public int PendingDocuments { get; set; }

    // Professor
    public int ClassesTaught { get; set; }
    public int AbsencesRecorded { get; set; }
    public int DocumentsToReview { get; set; }

    // Administrator
    public int StudentCount { get; set; }
    public int ProfessorCount { get; set; }
    public int ClassCount { get; set; }
    public int AllUnresolvedViolations { get; set; }

    public List<Violation> RecentViolations { get; set; } = new();
    public List<Absence> RecentAbsences { get; set; } = new();
    public List<ExcuseDocument> RecentDocuments { get; set; } = new();
}