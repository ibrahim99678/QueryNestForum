namespace QueryNest.Contract.Users;

public class UserProfileDto
{
    public int UserId { get; init; }
    public string AspNetUserId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? AvatarPath { get; init; }
    public string? Bio { get; init; }
    public int Reputation { get; init; }
}
