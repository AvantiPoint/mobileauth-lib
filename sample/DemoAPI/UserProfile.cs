using System.Security.Claims;

public static class UserProfile
{
    public static WebApplication MapUserProfile(this WebApplication app)
    {
        app.MapGet("/profile", GetProfile)
            .WithName("UserProfile")
            .RequireAuthorization();
        return app;
    }

    private static Task GetProfile(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = 200;
        var claims = context.User.Claims.ToDictionary(x => GetKey(x), x => x.Value);
        return context.Response.WriteAsJsonAsync(claims);
    }

    private static string GetKey(Claim claim) =>
        claim.Properties.Any() ? claim.Properties.First().Value : claim.Type;

}