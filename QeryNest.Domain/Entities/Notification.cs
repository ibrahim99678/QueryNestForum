using QueryNest.Domain.Enums;

namespace QueryNest.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public int ActorUserId { get; set; }
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public int? CommentId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public UserProfile User { get; set; } = default!;
    public UserProfile ActorUser { get; set; } = default!;
    public Question? Question { get; set; }
    public Answer? Answer { get; set; }
    public Comment? Comment { get; set; }
}
