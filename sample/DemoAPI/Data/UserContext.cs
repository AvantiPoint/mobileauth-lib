using Microsoft.EntityFrameworkCore;

namespace DemoAPI.Data;

public class UserContext : DbContext
{
    public UserContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<AuthorizedTokens> AuthorizedTokens { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }
}
