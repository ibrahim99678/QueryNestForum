namespace QueryNest.Contract.Users;

public class UpdateProfileRequestDto
{
    public string Name { get; init; } = default!;
    public string? Bio { get; init; }
}
