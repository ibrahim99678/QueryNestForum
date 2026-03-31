using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Auth;
using QueryNest.Contract.Notifications;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;
using QueryNest.Domain.Enums;

namespace QueryNest.BLL.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NotificationListDto> GetLatestAsync(string aspNetUserId, int take = 10, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 10;
        }

        take = Math.Min(take, 50);

        var profile = await GetUserProfileAsync(aspNetUserId, cancellationToken);
        if (profile is null)
        {
            return new NotificationListDto();
        }

        var unreadCount = await _unitOfWork.Notifications.Query()
            .AsNoTracking()
            .CountAsync(n => n.UserId == profile.UserId && !n.IsRead, cancellationToken);

        var items = await _unitOfWork.Notifications.Query()
            .AsNoTracking()
            .Where(n => n.UserId == profile.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                QuestionId = n.QuestionId
            })
            .ToListAsync(cancellationToken);

        return new NotificationListDto
        {
            Items = items,
            UnreadCount = unreadCount
        };
    }

    public async Task<int> GetUnreadCountAsync(string aspNetUserId, CancellationToken cancellationToken = default)
    {
        var profile = await GetUserProfileAsync(aspNetUserId, cancellationToken);
        if (profile is null)
        {
            return 0;
        }

        return await _unitOfWork.Notifications.Query()
            .AsNoTracking()
            .CountAsync(n => n.UserId == profile.UserId && !n.IsRead, cancellationToken);
    }

    public async Task<AuthResultDto> MarkReadAsync(string aspNetUserId, int notificationId, CancellationToken cancellationToken = default)
    {
        var profile = await GetUserProfileAsync(aspNetUserId, cancellationToken);
        if (profile is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var notification = await _unitOfWork.Notifications.Query()
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId, cancellationToken);

        if (notification is null || notification.UserId != profile.UserId)
        {
            return AuthResultDto.Failed("Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> MarkAllReadAsync(string aspNetUserId, CancellationToken cancellationToken = default)
    {
        var profile = await GetUserProfileAsync(aspNetUserId, cancellationToken);
        if (profile is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var items = await _unitOfWork.Notifications.Query()
            .Where(n => n.UserId == profile.UserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return AuthResultDto.Success();
        }

        var now = DateTime.UtcNow;
        foreach (var n in items)
        {
            n.IsRead = true;
            n.ReadAt = now;
            _unitOfWork.Notifications.Update(n);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }

    public async Task CreateAsync(int recipientUserId, int actorUserId, NotificationType type, string message, int? questionId, int? answerId, int? commentId, CancellationToken cancellationToken = default)
    {
        if (recipientUserId == actorUserId)
        {
            return;
        }

        var trimmed = (message ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        if (trimmed.Length > 500)
        {
            trimmed = trimmed[..500];
        }

        await _unitOfWork.Notifications.AddAsync(
            new Notification
            {
                UserId = recipientUserId,
                ActorUserId = actorUserId,
                Type = type,
                Message = trimmed,
                QuestionId = questionId,
                AnswerId = answerId,
                CommentId = commentId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private Task<UserProfile?> GetUserProfileAsync(string aspNetUserId, CancellationToken cancellationToken)
    {
        return _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.AspNetUserId == aspNetUserId, cancellationToken);
    }
}

