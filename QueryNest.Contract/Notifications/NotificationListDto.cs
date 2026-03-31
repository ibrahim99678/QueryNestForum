namespace QueryNest.Contract.Notifications;

public class NotificationListDto
{
    public IReadOnlyList<NotificationDto> Items { get; init; } = [];
    public int UnreadCount { get; init; }
}
