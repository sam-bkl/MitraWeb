namespace cos.Models;

public class UserCookieContext
{
    public string? Role { get; init; }
    public string? SSA { get; init; }
    public string? Circle { get; init; }

    public bool IsAuthenticated =>
        Role != null && SSA != null && Circle != null;
}