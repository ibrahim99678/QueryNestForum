using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Reports;
using QueryNest.Web.Models.Reports;

namespace QueryNest.Web.Controllers;

[Authorize(Roles = "Admin,Moderator")]
public class ModerationController : Controller
{
    private readonly IReportService _reportService;

    public ModerationController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _reportService.GetPendingAsync(cancellationToken);
        var model = items.Select(r => new ReportListItemViewModel
        {
            ReportId = r.ReportId,
            TargetType = r.TargetType.ToString(),
            TargetId = r.TargetId,
            Reason = r.Reason,
            Details = r.Details,
            CreatedAt = r.CreatedAt,
            ReporterName = r.ReporterName,
            ContentOwnerName = r.ContentOwnerName,
            ContentOwnerUserId = r.ContentOwnerUserId,
            QuestionId = r.QuestionId,
            ContentPreview = r.ContentPreview
        }).ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int reportId, bool approve, string? reviewNote, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        await _reportService.ReviewAsync(
            userId,
            new ReportReviewRequestDto
            {
                ReportId = reportId,
                Approve = approve,
                ReviewNote = reviewNote
            },
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContent(int reportId, string? reviewNote, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _reportService.DeleteReportedContentAsync(userId, reportId, reviewNote, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }
        else
        {
            TempData["Success"] = "Content deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ban(int userId, int days = 7, string? reason = null, CancellationToken cancellationToken = default)
    {
        var moderatorAspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(moderatorAspNetUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        await _reportService.BanUserAsync(moderatorAspNetUserId, userId, days, reason, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unban(int userId, CancellationToken cancellationToken = default)
    {
        var moderatorAspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(moderatorAspNetUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        await _reportService.UnbanUserAsync(moderatorAspNetUserId, userId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
