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

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var (result, answer) = await _answerService.GetForEditAsync(userId, id, cancellationToken);
        if (!result.Succeeded || answer is null)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
            return RedirectToAction("Details", "Questions", new { id });
        }

        return View(new AnswerEditViewModel
        {
            AnswerId = answer.AnswerId,
            QuestionId = answer.QuestionId,
            Content = answer.Content
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AnswerEditViewModel model, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (result, questionId) = await _answerService.UpdateAsync(
            userId,
            model.AnswerId,
            new AnswerUpdateRequestDto { Content = model.Content },
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
            return View(model);
        }

        TempData["Success"] = "Answer updated.";
        return RedirectToAction("Details", "Questions", new { id = questionId ?? model.QuestionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int questionId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var (result, qid) = await _answerService.DeleteAsync(userId, id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }
        else
        {
            TempData["Success"] = "Answer deleted.";
        }

        return RedirectToAction("Details", "Questions", new { id = qid ?? questionId });
    }
}
