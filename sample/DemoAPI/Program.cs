using AvantiPoint.MobileAuth;
using AvantiPoint.MobileAuth.Authentication;
using DemoAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddMobileAuth(auth =>
{
    // Configure override for Token Store
    auth.ConfigureDbTokenStore<UserContext>(o => o.UseInMemoryDatabase("DemoApi"));
    // Configure override for Claims Handler
    auth.AddMobileAuthClaimsHandler<CustomClaimsHandler>();

    // Add Additional Providers like Facebook, Twitter, LinkedIn, GitHub, etc...
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Map("/", async context =>
{
    await Task.CompletedTask;
    context.Response.Redirect("/swagger");
});

// maps https://{host}/mobileauth/{Apple|Google|Microsoft}
app.MapDefaultMobileAuthRoutes();
//app.MapMobileAuthRoute();
//app.MapMobileAuthLogoutRoute();
//app.MapMobileAuthUserClaimsRoute("/profile");

app.Run();
