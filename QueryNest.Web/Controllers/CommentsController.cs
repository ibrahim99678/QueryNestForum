using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Comments;
using QueryNest.Web.Models.Comments;

namespace QueryNest.Web.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommentCreateViewModel model, int questionId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Details", "Questions", new { id = questionId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var (result, resolvedQuestionId) = await _commentService.CreateAsync(
            userId,
            new CommentCreateRequestDto
            {
                AnswerId = model.AnswerId,
                ParentCommentId = model.ParentCommentId,
                Content = model.Content
            },
            cancellationToken);

        var redirectId = resolvedQuestionId ?? questionId;

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction("Details", "Questions", new { id = redirectId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var dto = await _commentService.GetForEditAsync(id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return View(new CommentEditViewModel
        {
            CommentId = dto.CommentId,
            QuestionId = dto.QuestionId,
            Content = dto.Content
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CommentEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var (result, questionId) = await _commentService.UpdateAsync(
            userId,
            model.CommentId,
            new CommentUpdateRequestDto { Content = model.Content },
            cancellationToken);

        var redirectId = questionId ?? model.QuestionId;

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction("Details", "Questions", new { id = redirectId });
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

        var (result, resolvedQuestionId) = await _commentService.DeleteAsync(userId, id, cancellationToken);
        var redirectId = resolvedQuestionId ?? questionId;

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction("Details", "Questions", new { id = redirectId });
    }
}

