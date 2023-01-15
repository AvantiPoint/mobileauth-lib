using AvantiPoint.MobileAuth;
using AvantiPoint.MobileAuth.Authentication;
using DemoAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Custom Overrides for Token Validation & User Claims
builder.Services.AddScoped<ITokenService, CustomTokenService>()
    .AddScoped<IMobileAuthClaimsHandler, CustomClaimsHandler>();

builder.AddMobileAuth(builder =>
{
    // Add Additional Providers like Facebook, Twitter, LinkedIn, GitHub, etc...
});

builder.Services.AddDbContext<UserContext>(o => o.UseInMemoryDatabase("DemoApi"));

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
app.MapDefaultMobileAuthRoutes();
//app.MapMobileAuthRoute();
//app.MapMobileAuthLogoutRoute();
//app.MapMobileAuthUserClaimsRoute("/profile");

app.Run();
