using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Answers;
using QueryNest.Web.Models.Answers;

namespace QueryNest.Web.Controllers;

[Authorize]
public class AnswersController : Controller
{
    private readonly IAnswerService _answerService;

    public AnswersController(IAnswerService answerService)
    {
        _answerService = answerService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnswerCreateViewModel model, CancellationToken cancellationToken)
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

        var (result, _) = await _answerService.CreateAsync(
            userId,
            new AnswerCreateRequestDto
            {
                QuestionId = model.QuestionId,
                Content = model.Content
            },
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction("Details", "Questions", new { id = model.QuestionId });
    }
}

