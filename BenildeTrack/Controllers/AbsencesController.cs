using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Professors record absences for their own classes. Students can view their own.
[Authorize(Roles = AppRoles.Professor + "," + AppRoles.Student)]
public class AbsencesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AbsencesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private IQueryable<Absence> ScopedQuery()
    {
        var myId = _userManager.GetUserId(User);
        var query = _db.Absences
            .Include(a => a.Student)
            .Include(a => a.Class)
            .Include(a => a.ExcuseDocument)
            .AsQueryable();
        if (User.IsInRole(AppRoles.Professor))
            query = query.Where(a => a.Class!.ProfessorId == myId);
        else
            query = query.Where(a => a.StudentId == myId);
        return query;
    }

    private async Task LoadListsAsync(Absence? current = null)
    {
        var myId = _userManager.GetUserId(User);

        var students = await _userManager.GetUsersInRoleAsync(AppRoles.Student);
        ViewBag.Students = new SelectList(students.OrderBy(s => s.FullName), "Id", "FullName", current?.StudentId);

        var classes = await _db.Classes.Where(c => c.ProfessorId == myId).OrderBy(c => c.CourseCode).ToListAsync();
        ViewBag.Classes = new SelectList(
            classes.Select(c => new { c.Id, Label = $"{c.CourseCode} {c.Section} - {c.CourseName}" }),
            "Id", "Label", current?.ClassId);

        var docs = await _db.ExcuseDocuments.Include(d => d.Student)
            .Where(d => d.ProfessorId == myId && (d.Status == "Accepted" || d.Status == "Acknowledged"))
            .OrderByDescending(d => d.DateSubmitted).ToListAsync();
        ViewBag.Documents = new SelectList(
            docs.Select(d => new { d.Id, Label = $"{d.Student?.FullName} - {d.DocumentType} ({d.DateSubmitted:yyyy-MM-dd})" }),
            "Id", "Label", current?.ExcuseDocumentId);
    }

    public async Task<IActionResult> Index()
    {
        return View(await ScopedQuery().OrderByDescending(a => a.AbsenceDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var item = await ScopedQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View(new Absence());
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("StudentId,ClassId,AbsenceDate,ExcuseDocumentId")] Absence model)
    {
        await ValidateAsync(model, null);
        if (!ModelState.IsValid)
        {
            await LoadListsAsync(model);
            return View(model);
        }
        _db.Absences.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var item = await ScopedQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (item == null) return NotFound();
        await LoadListsAsync(item);
        return View(item);
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,ClassId,AbsenceDate,ExcuseDocumentId")] Absence model)
    {
        if (id != model.Id) return NotFound();
        var existing = await ScopedQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (existing == null) return NotFound();

        await ValidateAsync(model, id);
        if (!ModelState.IsValid)
        {
            await LoadListsAsync(model);
            return View(model);
        }

        // Tracked entity came from ScopedQuery with Includes; update plain fields only.
        existing.StudentId = model.StudentId;
        existing.ClassId = model.ClassId;
        existing.AbsenceDate = model.AbsenceDate;
        existing.ExcuseDocumentId = model.ExcuseDocumentId;
        existing.Student = null;
        existing.Class = null;
        existing.ExcuseDocument = null;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var item = await ScopedQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await ScopedQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (item != null)
        {
            _db.Absences.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(Absence model, int? editingId)
    {
        var myId = _userManager.GetUserId(User);

        var student = string.IsNullOrEmpty(model.StudentId) ? null : await _userManager.FindByIdAsync(model.StudentId);
        if (student == null || !await _userManager.IsInRoleAsync(student, AppRoles.Student))
            ModelState.AddModelError(nameof(model.StudentId), "Pick a valid student.");

        if (!await _db.Classes.AnyAsync(c => c.Id == model.ClassId && c.ProfessorId == myId))
            ModelState.AddModelError(nameof(model.ClassId), "Pick one of your own classes.");

        if (model.AbsenceDate.Date > DateTime.Today)
            ModelState.AddModelError(nameof(model.AbsenceDate), "Absence date can't be in the future.");

        if (await _db.Absences.AnyAsync(a => a.Id != editingId && a.StudentId == model.StudentId
                && a.ClassId == model.ClassId && a.AbsenceDate.Date == model.AbsenceDate.Date))
            ModelState.AddModelError(string.Empty, "This absence is already recorded for that student, class and date.");

        if (model.ExcuseDocumentId != null)
        {
            var doc = await _db.ExcuseDocuments.FindAsync(model.ExcuseDocumentId);
            if (doc == null || doc.StudentId != model.StudentId || doc.ProfessorId != myId
                || (doc.Status != "Accepted" && doc.Status != "Acknowledged"))
                ModelState.AddModelError(nameof(model.ExcuseDocumentId),
                    "Pick an accepted or acknowledged document from this student that was addressed to you.");
        }
    }
}