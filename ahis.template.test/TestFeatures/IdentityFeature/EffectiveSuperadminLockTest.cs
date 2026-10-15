using ahis.template.identity.Contexts;
using ahis.template.identity.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public sealed class EffectiveSuperadminLockTest
{
    [Fact]
    public async Task AcquisitionFailsClosedWithoutAnExistingTransaction()
    {
        var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>()
            .UseSqlServer("Server=localhost;Database=not-used;Trusted_Connection=True")
            .Options);
        try
        {
            var acquired = await new EffectiveSuperadminLock(context).AcquireAsync(CancellationToken.None);

            acquired.Should().BeFalse();
            context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
}
