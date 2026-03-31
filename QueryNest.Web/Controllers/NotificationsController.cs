using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Web.Models.Notifications;

namespace QueryNest.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var dto = await _notificationService.GetLatestAsync(userId, 50, cancellationToken);
        var model = new NotificationListViewModel
        {
            UnreadCount = dto.UnreadCount,
            Items = dto.Items.Select(n => new NotificationItemViewModel
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                QuestionId = n.QuestionId
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LatestPartial(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var dto = await _notificationService.GetLatestAsync(userId, 10, cancellationToken);
        var model = new NotificationListViewModel
        {
            UnreadCount = dto.UnreadCount,
            Items = dto.Items.Select(n => new NotificationItemViewModel
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                QuestionId = n.QuestionId
            }).ToList()
        };

        return PartialView("_Dropdown", model);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Json(new { count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        await _notificationService.MarkReadAsync(userId, id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        await _notificationService.MarkAllReadAsync(userId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}

