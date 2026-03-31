namespace QueryNest.Contract.Dashboard;

public class TopUserDto
{
    public int UserId { get; init; }
    public string Name { get; init; } = default!;
    public int Reputation { get; init; }
    public int Questions { get; init; }
    public int Answers { get; init; }
}
