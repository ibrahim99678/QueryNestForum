namespace QueryNest.Contract.Notifications;

public class NotificationDto
{
    public int NotificationId { get; init; }
    public string Message { get; init; } = default!;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public int? QuestionId { get; init; }
}
