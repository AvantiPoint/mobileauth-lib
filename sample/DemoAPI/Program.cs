using System.Text.RegularExpressions;
using AvantiPoint.MobileAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddMobileAuth(builder =>
{
    // Add Additional Providers like Facebook, Twitter, LinkedIn, GitHub, etc...
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// maps https://{host}/mobileauth/{Apple|Google|Microsoft}
app.MapMobileAuthRoute();
app.MapGet("profile", async context =>
{
    context.Response.StatusCode = 200;
    var userClaims = context.User.Claims;
    var claim = userClaims.First();
    var claims = context.User.Claims.ToDictionary(x => x.Properties.Any() ? x.Properties.First().Value : x.Type, x => x.Value);
    await context.Response.WriteAsJsonAsync(claims);
})
    .RequireAuthorization();

app.Run();

// If needed provide this as a Scoped Service.
public class CustomClaimsHandler : MobileAuthClaimsHandler
{
    public override ValueTask<Dictionary<string, string>> GenerateClaims(HttpContext context, AuthenticateResult auth, string scheme)
    {
        var claims = base.GenerateClaims(context, auth, scheme);

        // Need to update a database or specify specific claims? You can do that here...

        return claims;
    }
}