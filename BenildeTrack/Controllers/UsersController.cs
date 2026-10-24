using BenildeTrack.Data;
using BenildeTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Covers both "User Accounts" and "Students": students are users with the Student role.
// Use /Users?role=Student to see only students.
[Authorize(Roles = AppRoles.Administrator)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    private SelectList RoleList(string? selected = null) => new SelectList(AppRoles.All, selected);

    public async Task<IActionResult> Index(string? role)
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var items = new List<UserListItem>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var userRole = roles.FirstOrDefault() ?? "";
            if (!string.IsNullOrEmpty(role) && userRole != role) continue;
            items.Add(new UserListItem { Id = u.Id, FullName = u.FullName, Email = u.Email ?? "", Role = userRole });
        }
        ViewBag.Roles = RoleList(role);
        ViewBag.RoleFilter = role;
        return View(items);
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return View(new UserListItem { Id = user.Id, FullName = user.FullName, Email = user.Email ?? "", Role = roles.FirstOrDefault() ?? "" });
    }

    [HttpGet]
    public IActionResult Create(string? role)
    {
        ViewBag.Roles = RoleList(role ?? AppRoles.Student);
        return View(new UserCreateViewModel { Role = role ?? AppRoles.Student });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        ViewBag.Roles = RoleList(model.Role);
        if (!AppRoles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }
        await _userManager.AddToRoleAsync(user, model.Role);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRoles.Student;
        ViewBag.Roles = RoleList(role);
        return View(new UserEditViewModel { Id = user.Id, FullName = user.FullName, Email = user.Email ?? "", Role = role });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (id != model.Id) return NotFound();
        ViewBag.Roles = RoleList(model.Role);
        if (!AppRoles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Don't let the logged-in admin demote themselves and lock everyone out.
        if (user.Id == _userManager.GetUserId(User) && model.Role != AppRoles.Administrator)
            ModelState.AddModelError(nameof(model.Role), "You can't change your own role.");

        if (!ModelState.IsValid) return View(model);

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role) || currentRoles.Count > 1)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pwResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!pwResult.Succeeded)
            {
                foreach (var e in pwResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return View(new UserListItem { Id = user.Id, FullName = user.FullName, Email = user.Email ?? "", Role = roles.FirstOrDefault() ?? "" });
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return RedirectToAction(nameof(Index));

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You can't delete your own account.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Violations.AnyAsync(v => v.StudentId == id) ||
            await _db.Absences.AnyAsync(a => a.StudentId == id) ||
            await _db.ExcuseDocuments.AnyAsync(d => d.StudentId == id || d.ProfessorId == id))
        {
            TempData["Error"] = "This user still has violations, absences, or excuse documents on record. Delete those first.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        return RedirectToAction(nameof(Index));
    }
}