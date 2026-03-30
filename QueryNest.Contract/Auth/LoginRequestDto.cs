namespace QueryNest.Contract.Auth;

public class LoginRequestDto
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public bool RememberMe { get; init; }
}
