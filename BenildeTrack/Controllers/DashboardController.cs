using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Read-only summary of data from the other modules, depending on the logged-in role.
[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var myId = user.Id;
        var vm = new DashboardViewModel { FullName = user.FullName };

        if (User.IsInRole(AppRoles.Administrator))
        {
            vm.Role = AppRoles.Administrator;
            vm.StudentCount = (await _userManager.GetUsersInRoleAsync(AppRoles.Student)).Count;
            vm.ProfessorCount = (await _userManager.GetUsersInRoleAsync(AppRoles.Professor)).Count;
            vm.ClassCount = await _db.Classes.CountAsync();
            vm.AllUnresolvedViolations = await _db.Violations.CountAsync(v => v.Status == "Unresolved");
            vm.RecentViolations = await _db.Violations.Include(v => v.Student)
                .OrderByDescending(v => v.ViolationDate).Take(5).ToListAsync();
        }
        else if (User.IsInRole(AppRoles.Professor))
        {
            vm.Role = AppRoles.Professor;
            vm.ClassesTaught = await _db.Classes.CountAsync(c => c.ProfessorId == myId);
            vm.AbsencesRecorded = await _db.Absences.CountAsync(a => a.Class!.ProfessorId == myId);
            vm.DocumentsToReview = await _db.ExcuseDocuments.CountAsync(d => d.ProfessorId == myId && d.Status == "Pending");
            vm.RecentDocuments = await _db.ExcuseDocuments.Include(d => d.Student)
                .Where(d => d.ProfessorId == myId && d.Status == "Pending")
                .OrderByDescending(d => d.DateSubmitted).Take(5).ToListAsync();
        }
        else if (User.IsInRole(AppRoles.Student))
        {
            vm.Role = AppRoles.Student;
            vm.TotalViolations = await _db.Violations.CountAsync(v => v.StudentId == myId);
            vm.UnresolvedViolations = await _db.Violations.CountAsync(v => v.StudentId == myId && v.Status == "Unresolved");
            vm.TotalAbsences = await _db.Absences.CountAsync(a => a.StudentId == myId);
            vm.PendingDocuments = await _db.ExcuseDocuments.CountAsync(d => d.StudentId == myId && d.Status == "Pending");
            vm.RecentViolations = await _db.Violations
                .Where(v => v.StudentId == myId).OrderByDescending(v => v.ViolationDate).Take(5).ToListAsync();
            vm.RecentAbsences = await _db.Absences.Include(a => a.Class)
                .Where(a => a.StudentId == myId).OrderByDescending(a => a.AbsenceDate).Take(5).ToListAsync();
            vm.RecentDocuments = await _db.ExcuseDocuments.Include(d => d.Professor)
                .Where(d => d.StudentId == myId).OrderByDescending(d => d.DateSubmitted).Take(5).ToListAsync();
        }

        return View(vm);
    }
}