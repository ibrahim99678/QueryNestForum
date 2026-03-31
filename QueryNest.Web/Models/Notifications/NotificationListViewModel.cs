namespace QueryNest.Web.Models.Notifications;

public class NotificationListViewModel
{
    public List<NotificationItemViewModel> Items { get; set; } = [];
    public int UnreadCount { get; set; }
}
