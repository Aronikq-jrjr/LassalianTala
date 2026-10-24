namespace BenildeTrack.Models;

public static class AppRoles
{
    public const string Student = "Student";
    public const string Professor = "Professor";
    public const string Administrator = "Administrator";

    public static readonly string[] All = { Student, Professor, Administrator };
}