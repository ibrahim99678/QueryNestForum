namespace QueryNest.Web.Models.Dashboard;

public class TopUserViewModel
{
    public int UserId { get; set; }
    public string Name { get; set; } = default!;
    public int Reputation { get; set; }
    public int Questions { get; set; }
    public int Answers { get; set; }
}
