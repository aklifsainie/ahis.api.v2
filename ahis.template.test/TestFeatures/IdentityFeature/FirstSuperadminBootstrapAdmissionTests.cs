using FluentAssertions;
using System.Diagnostics;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public sealed class FirstSuperadminBootstrapAdmissionTests
{
    [Fact]
    public async Task BootstrapCommandWithMissingConfirmationRefusesBeforeHostStartup()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var start = new ProcessStartInfo("dotnet", "run --no-build --project ahis.template.api -- bootstrap-superadmin --user-id test-user")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        process.ExitCode.Should().NotBe(0);
    }
}
