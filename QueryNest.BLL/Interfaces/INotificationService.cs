using QueryNest.Contract.Auth;
using QueryNest.Contract.Notifications;
using QueryNest.Domain.Enums;

namespace QueryNest.BLL.Interfaces;

public interface INotificationService
{
    Task<NotificationListDto> GetLatestAsync(string aspNetUserId, int take = 10, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string aspNetUserId, CancellationToken cancellationToken = default);
    Task<AuthResultDto> MarkReadAsync(string aspNetUserId, int notificationId, CancellationToken cancellationToken = default);
    Task<AuthResultDto> MarkAllReadAsync(string aspNetUserId, CancellationToken cancellationToken = default);
    Task CreateAsync(int recipientUserId, int actorUserId, NotificationType type, string message, int? questionId, int? answerId, int? commentId, CancellationToken cancellationToken = default);
}
