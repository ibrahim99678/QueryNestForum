using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Reports;
using QueryNest.Web.Models.Reports;

namespace QueryNest.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReportCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Details", "Questions", new { id = model.ReturnQuestionId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _reportService.CreateAsync(
            userId,
            new ReportCreateRequestDto
            {
                TargetType = (ReportTargetTypeDto)model.TargetType,
                TargetId = model.TargetId,
                Reason = model.Reason,
                Details = model.Details
            },
            cancellationToken);

        TempData["ReportMessage"] = result.Succeeded ? "Report submitted." : string.Join(" ", result.Errors);

        return RedirectToAction("Details", "Questions", new { id = model.ReturnQuestionId });
    }
}

