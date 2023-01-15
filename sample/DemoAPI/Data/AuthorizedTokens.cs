namespace DemoAPI.Data;

public class AuthorizedTokens
{
    public Guid Id { get; set; }

    public string Token { get; set; } = default!;
}
