using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Votes;
using QueryNest.Web.Models.Votes;

namespace QueryNest.Web.Controllers;

[Authorize]
public class VotesController : Controller
{
    private readonly IVoteService _voteService;

    public VotesController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cast(VoteCastViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Details", "Questions", new { id = model.QuestionId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var (result, resolvedQuestionId) = await _voteService.CastAsync(
            userId,
            new CastVoteRequestDto
            {
                TargetType = (VoteTargetTypeDto)model.TargetType,
                TargetId = model.TargetId,
                VoteType = model.VoteType
            },
            cancellationToken);

        var redirectId = resolvedQuestionId ?? model.QuestionId;

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction("Details", "Questions", new { id = redirectId });
    }
}

