using Microsoft.EntityFrameworkCore;

namespace CompanyName.MyMeetings.Modules.UserAccess.Infrastructure.OpenIddict;

public class OpenIddictDbContext : DbContext
{
    public OpenIddictDbContext(DbContextOptions<OpenIddictDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");
        base.OnModelCreating(modelBuilder);
    }
}
