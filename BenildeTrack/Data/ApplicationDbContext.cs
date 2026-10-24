using BenildeTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Violation> Violations => Set<Violation>();
    public DbSet<Absence> Absences => Set<Absence>();
    public DbSet<ExcuseDocument> ExcuseDocuments => Set<ExcuseDocument>();

    public DbSet<HandbookOffense> HandbookOffenses => Set<HandbookOffense>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Violation>()
            .HasOne(v => v.Student).WithMany()
            .HasForeignKey(v => v.StudentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SchoolClass>()
            .HasOne(c => c.Professor).WithMany()
            .HasForeignKey(c => c.ProfessorId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Absence>()
            .HasOne(a => a.Student).WithMany()
            .HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Absence>()
            .HasOne(a => a.Class).WithMany()
            .HasForeignKey(a => a.ClassId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Absence>()
            .HasOne(a => a.ExcuseDocument).WithMany()
            .HasForeignKey(a => a.ExcuseDocumentId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ExcuseDocument>()
            .HasOne(d => d.Student).WithMany()
            .HasForeignKey(d => d.StudentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ExcuseDocument>()
            .HasOne(d => d.Professor).WithMany()
            .HasForeignKey(d => d.ProfessorId).OnDelete(DeleteBehavior.Restrict);
    }
}