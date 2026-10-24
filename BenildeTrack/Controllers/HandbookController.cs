using System.Collections.Concurrent;
using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Controllers;

// Students ask questions about the handbook. Nothing is saved.
[Authorize(Roles = AppRoles.Student)]
public class HandbookController : Controller
{
    private const int MaxQuestionsPerHour = 20;
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> RecentQuestions = new();

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HandbookAssistant _assistant;

    public HandbookController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, HandbookAssistant assistant)
    {
        _db = db;
        _userManager = userManager;
        _assistant = assistant;
    }

    [HttpGet]
    public IActionResult Index() => View(new HandbookAskViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HandbookAskViewModel model)
    {
        model.Answer = null;
        model.Error = null;
        if (!ModelState.IsValid) return View(model);

        var userId = _userManager.GetUserId(User)!;
        if (!AllowRequest(userId))
        {
            model.Error = "You've asked a lot of questions this hour. Please try again later.";
            return View(model);
        }

        var offenses = await _db.HandbookOffenses.OrderBy(o => o.Id).ToListAsync();
        var reply = await _assistant.AskAsync(model.Question, offenses);
        model.Answer = reply.Answer;
        model.Error = reply.Error;
        return View(model);
    }

    // Simple in-memory limit so one student can't use up the shared API quota.
    private static bool AllowRequest(string userId)
    {
        var queue = RecentQuestions.GetOrAdd(userId, _ => new Queue<DateTime>());
        lock (queue)
        {
            var cutoff = DateTime.UtcNow.AddHours(-1);
            while (queue.Count > 0 && queue.Peek() < cutoff) queue.Dequeue();
            if (queue.Count >= MaxQuestionsPerHour) return false;
            queue.Enqueue(DateTime.UtcNow);
            return true;
        }
    }
}