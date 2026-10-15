using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ahis.template.identity.Contexts;

public sealed class IdentityContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityContext>
{
    public IdentityContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseSqlServer("Server=localhost;Database=IdentityMigrationDesignTime;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new IdentityContext(options);
    }
}
