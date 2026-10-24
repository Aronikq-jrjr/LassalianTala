using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class ClassesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClassesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task LoadProfessorsAsync(string? selected = null)
    {
        var professors = await _userManager.GetUsersInRoleAsync(AppRoles.Professor);
        ViewBag.Professors = new SelectList(professors.OrderBy(p => p.FullName), "Id", "FullName", selected);
    }

    public async Task<IActionResult> Index()
    {
        var classes = await _db.Classes.Include(c => c.Professor)
            .OrderBy(c => c.CourseCode).ThenBy(c => c.Section).ToListAsync();
        return View(classes);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.Classes.Include(c => c.Professor).FirstOrDefaultAsync(c => c.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadProfessorsAsync();
        return View(new SchoolClass());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CourseCode,CourseName,Section,ProfessorId")] SchoolClass model)
    {
        if (!await ProfessorIsValid(model.ProfessorId))
            ModelState.AddModelError(nameof(model.ProfessorId), "Pick a valid professor.");
        if (await _db.Classes.AnyAsync(c => c.CourseCode == model.CourseCode && c.Section == model.Section))
            ModelState.AddModelError(nameof(model.Section), "That course code and section already exists.");

        if (!ModelState.IsValid)
        {
            await LoadProfessorsAsync(model.ProfessorId);
            return View(model);
        }
        _db.Classes.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.Classes.FindAsync(id);
        if (item == null) return NotFound();
        await LoadProfessorsAsync(item.ProfessorId);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,CourseCode,CourseName,Section,ProfessorId")] SchoolClass model)
    {
        if (id != model.Id) return NotFound();
        if (!await ProfessorIsValid(model.ProfessorId))
            ModelState.AddModelError(nameof(model.ProfessorId), "Pick a valid professor.");
        if (await _db.Classes.AnyAsync(c => c.Id != model.Id && c.CourseCode == model.CourseCode && c.Section == model.Section))
            ModelState.AddModelError(nameof(model.Section), "That course code and section already exists.");

        if (!ModelState.IsValid)
        {
            await LoadProfessorsAsync(model.ProfessorId);
            return View(model);
        }

        var existing = await _db.Classes.FindAsync(id);
        if (existing == null) return NotFound();
        existing.CourseCode = model.CourseCode;
        existing.CourseName = model.CourseName;
        existing.Section = model.Section;
        existing.ProfessorId = model.ProfessorId;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.Classes.Include(c => c.Professor).FirstOrDefaultAsync(c => c.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (await _db.Absences.AnyAsync(a => a.ClassId == id))
        {
            TempData["Error"] = "This class still has absence records. Delete those first.";
            return RedirectToAction(nameof(Index));
        }

        var item = await _db.Classes.FindAsync(id);
        if (item != null)
        {
            _db.Classes.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ProfessorIsValid(string? professorId)
    {
        if (string.IsNullOrEmpty(professorId)) return true; // professor is optional
        var user = await _userManager.FindByIdAsync(professorId);
        return user != null && await _userManager.IsInRoleAsync(user, AppRoles.Professor);
    }
}