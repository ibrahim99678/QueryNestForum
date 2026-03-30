namespace QueryNest.Contract.Auth;

public class AuthResultDto
{
    public bool Succeeded { get; init; }
    public string[] Errors { get; init; } = [];

    public static AuthResultDto Success()
    {
        return new AuthResultDto { Succeeded = true };
    }

    public static AuthResultDto Failed(params string[] errors)
    {
        return new AuthResultDto { Succeeded = false, Errors = errors };
    }
}
