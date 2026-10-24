using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Administrators manage violations. Students can only view their own.
[Authorize]
public class ViolationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ViolationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task LoadListsAsync(string? selectedStudent = null, string? selectedStatus = null)
    {
        var students = await _userManager.GetUsersInRoleAsync(AppRoles.Student);
        ViewBag.Students = new SelectList(students.OrderBy(s => s.FullName), "Id", "FullName", selectedStudent);
        ViewBag.Statuses = new SelectList(Violation.Statuses, selectedStatus);
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Student)]
    public async Task<IActionResult> Index()
    {
        var query = _db.Violations.Include(v => v.Student).AsQueryable();
        if (User.IsInRole(AppRoles.Student))
        {
            var myId = _userManager.GetUserId(User);
            query = query.Where(v => v.StudentId == myId);
        }
        return View(await query.OrderByDescending(v => v.ViolationDate).ToListAsync());
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Student)]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.Violations.Include(v => v.Student).FirstOrDefaultAsync(v => v.Id == id);
        if (item == null) return NotFound();
        if (User.IsInRole(AppRoles.Student) && item.StudentId != _userManager.GetUserId(User)) return Forbid();
        return View(item);
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View(new Violation());
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("StudentId,Offense,Description,ViolationDate,Status")] Violation model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadListsAsync(model.StudentId, model.Status);
            return View(model);
        }
        _db.Violations.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.Violations.FindAsync(id);
        if (item == null) return NotFound();
        await LoadListsAsync(item.StudentId, item.Status);
        return View(item);
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,Offense,Description,ViolationDate,Status")] Violation model)
    {
        if (id != model.Id) return NotFound();
        await ValidateAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadListsAsync(model.StudentId, model.Status);
            return View(model);
        }

        var existing = await _db.Violations.FindAsync(id);
        if (existing == null) return NotFound();
        existing.StudentId = model.StudentId;
        existing.Offense = model.Offense;
        existing.Description = model.Description;
        existing.ViolationDate = model.ViolationDate;
        existing.Status = model.Status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.Violations.Include(v => v.Student).FirstOrDefaultAsync(v => v.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.Violations.FindAsync(id);
        if (item != null)
        {
            _db.Violations.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(Violation model)
    {
        var student = string.IsNullOrEmpty(model.StudentId) ? null : await _userManager.FindByIdAsync(model.StudentId);
        if (student == null || !await _userManager.IsInRoleAsync(student, AppRoles.Student))
            ModelState.AddModelError(nameof(model.StudentId), "Pick a valid student.");
        if (!Violation.Statuses.Contains(model.Status))
            ModelState.AddModelError(nameof(model.Status), "Invalid status.");
        if (model.ViolationDate.Date > DateTime.Today)
            ModelState.AddModelError(nameof(model.ViolationDate), "Violation date can't be in the future.");
    }
}