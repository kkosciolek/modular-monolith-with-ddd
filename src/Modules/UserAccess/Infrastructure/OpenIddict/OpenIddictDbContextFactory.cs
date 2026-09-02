using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CompanyName.MyMeetings.Modules.UserAccess.Infrastructure.OpenIddict;

public class OpenIddictDbContextFactory : IDesignTimeDbContextFactory<OpenIddictDbContext>
{
    public OpenIddictDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OpenIddictDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=MyMeetings;Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("OpenIddictMigrationsHistory", "auth"));
        optionsBuilder.UseOpenIddict();

        return new OpenIddictDbContext(optionsBuilder.Options);
    }
}
