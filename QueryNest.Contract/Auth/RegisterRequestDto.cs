namespace QueryNest.Contract.Auth;

public class RegisterRequestDto
{
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}
