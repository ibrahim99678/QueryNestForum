namespace QueryNest.Web.Models.Notifications;

public class NotificationItemViewModel
{
    public int NotificationId { get; set; }
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? QuestionId { get; set; }
}
