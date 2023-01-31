using System.Diagnostics;
using AvantiPoint.MobileAuth;
using DemoAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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
var versionInfo = FileVersionInfo.GetVersionInfo(typeof(MobileAuth).Assembly.Location);
var version = $"{versionInfo.FileMajorPart}.{versionInfo.ProductMinorPart}.{versionInfo.ProductBuildPart}";
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc($"v1", new OpenApiInfo
    {
        Title = "Mobile Auth - Demo",
        Contact = new OpenApiContact
        {
            Name = "AvantiPoint",
            Email = "hello@avantipoint.com",
            Url = new Uri("https://avantipoint.com")
        },
        Description = "This is a demo api for the AvantiPoint Mobile Auth library. Do not use this API for production. For more information please visit https://github.com/avantipoint/mobileauth-lib",
        Version = version
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.InjectStylesheet("https://cdn.avantipoint.com/theme/swagger/style.css");
});

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
