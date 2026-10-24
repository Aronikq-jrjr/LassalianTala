using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Students upload and withdraw (while Pending). Professors review documents addressed to them.
[Authorize(Roles = AppRoles.Professor + "," + AppRoles.Student)]
public class ExcuseDocumentsController : Controller
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ExcuseDocumentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    private string UploadDir => Path.Combine(_env.ContentRootPath, "App_Data", "uploads");

    private IQueryable<ExcuseDocument> ScopedQuery()
    {
        var myId = _userManager.GetUserId(User);
        var query = _db.ExcuseDocuments.Include(d => d.Student).Include(d => d.Professor).AsQueryable();
        if (User.IsInRole(AppRoles.Professor))
            query = query.Where(d => d.ProfessorId == myId);
        else
            query = query.Where(d => d.StudentId == myId);
        return query;
    }

    private static string ContentTypeFor(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

    private async Task LoadUploadListsAsync(ExcuseUploadViewModel? model = null)
    {
        var professors = await _userManager.GetUsersInRoleAsync(AppRoles.Professor);
        ViewBag.Professors = new SelectList(professors.OrderBy(p => p.FullName), "Id", "FullName", model?.ProfessorId);
        ViewBag.Types = new SelectList(ExcuseDocument.Types, model?.DocumentType);
    }

    public async Task<IActionResult> Index()
    {
        return View(await ScopedQuery().OrderByDescending(d => d.DateSubmitted).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var item = await ScopedQuery().FirstOrDefaultAsync(d => d.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    public async Task<IActionResult> Download(int id)
    {
        var doc = await ScopedQuery().FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound();
        var path = Path.Combine(UploadDir, doc.FilePath);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, ContentTypeFor(doc.FilePath));
    }

    // ---------- CREATE (student upload) ----------
    [Authorize(Roles = AppRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ExcuseUploadViewModel();
        await LoadUploadListsAsync(model);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExcuseUploadViewModel model)
    {
        var professor = string.IsNullOrEmpty(model.ProfessorId) ? null : await _userManager.FindByIdAsync(model.ProfessorId);
        if (professor == null || !await _userManager.IsInRoleAsync(professor, AppRoles.Professor))
            ModelState.AddModelError(nameof(model.ProfessorId), "Pick a valid professor.");

        if (!ExcuseDocument.Types.Contains(model.DocumentType))
            ModelState.AddModelError(nameof(model.DocumentType), "Invalid document type.");

        string ext = "";
        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Choose a file to upload.");
        }
        else
        {
            ext = Path.GetExtension(model.File.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                ModelState.AddModelError(nameof(model.File), "Only PDF, JPG or PNG files are allowed.");
            if (model.File.Length > MaxBytes)
                ModelState.AddModelError(nameof(model.File), "File is larger than 5 MB.");
        }

        if (model.DocumentType == ExcuseDocument.BenireType)
        {
            if (model.BenireStartDate == null)
                ModelState.AddModelError(nameof(model.BenireStartDate), "Enter the Benire start date.");
            if (model.BenireEndDate == null)
                ModelState.AddModelError(nameof(model.BenireEndDate), "Enter the Benire end date.");
            if (model.BenireStartDate != null && model.BenireEndDate != null && model.BenireEndDate < model.BenireStartDate)
                ModelState.AddModelError(nameof(model.BenireEndDate), "End date can't be before the start date.");
        }
        else
        {
            model.BenireStartDate = null;
            model.BenireEndDate = null;
        }

        if (!ModelState.IsValid)
        {
            await LoadUploadListsAsync(model);
            return View(model);
        }

        Directory.CreateDirectory(UploadDir);
        var storedName = Guid.NewGuid().ToString("N") + ext;
        using (var stream = new FileStream(Path.Combine(UploadDir, storedName), FileMode.Create))
        {
            await model.File!.CopyToAsync(stream);
        }

        var doc = new ExcuseDocument
        {
            StudentId = _userManager.GetUserId(User)!,
            ProfessorId = model.ProfessorId,
            DocumentType = model.DocumentType,
            BenireStartDate = model.BenireStartDate,
            BenireEndDate = model.BenireEndDate,
            FilePath = storedName,
            OriginalFileName = Path.GetFileName(model.File.FileName),
            DateSubmitted = DateTime.Now,
            Status = "Pending"
        };
        _db.ExcuseDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ---------- UPDATE (professor review / acknowledge) ----------
    private static string[] DecisionsFor(ExcuseDocument doc) =>
        doc.DocumentType == ExcuseDocument.BenireType
            ? new[] { "Acknowledged" }
            : new[] { "Accepted", "Rejected" };

    [Authorize(Roles = AppRoles.Professor)]
    [HttpGet]
    public async Task<IActionResult> Review(int? id)
    {
        if (id == null) return NotFound();
        var doc = await ScopedQuery().FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound();

        var decisions = DecisionsFor(doc);
        ViewBag.Document = doc;
        ViewBag.Decisions = new SelectList(decisions);
        return View(new ReviewViewModel { Id = doc.Id, Decision = decisions[0], Remarks = doc.Remarks });
    }

    [Authorize(Roles = AppRoles.Professor)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, ReviewViewModel model)
    {
        if (id != model.Id) return NotFound();
        var doc = await ScopedQuery().FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound();

        var decisions = DecisionsFor(doc);
        if (!decisions.Contains(model.Decision))
            ModelState.AddModelError(nameof(model.Decision), "Invalid decision for this document type.");
        if (model.Decision == "Rejected" && string.IsNullOrWhiteSpace(model.Remarks))
            ModelState.AddModelError(nameof(model.Remarks), "Add a remark explaining the rejection.");

        if (!ModelState.IsValid)
        {
            ViewBag.Document = doc;
            ViewBag.Decisions = new SelectList(decisions, model.Decision);
            return View(model);
        }

        doc.Status = model.Decision;
        doc.Remarks = model.Remarks;
        doc.SignedAt = model.Decision == "Acknowledged" ? DateTime.Now : null;
        doc.Student = null;
        doc.Professor = null;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ---------- DELETE (student withdraws a Pending document) ----------
    [Authorize(Roles = AppRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var doc = await ScopedQuery().FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound();
        if (doc.Status != "Pending")
        {
            TempData["Error"] = "Only pending documents can be withdrawn.";
            return RedirectToAction(nameof(Index));
        }
        return View(doc);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var doc = await ScopedQuery().FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return RedirectToAction(nameof(Index));
        if (doc.Status != "Pending")
        {
            TempData["Error"] = "Only pending documents can be withdrawn.";
            return RedirectToAction(nameof(Index));
        }

        var path = Path.Combine(UploadDir, doc.FilePath);
        _db.ExcuseDocuments.Remove(doc);
        await _db.SaveChangesAsync();
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        return RedirectToAction(nameof(Index));
    }
}