using System.Data;
using System.Text.Json;
using ahis.template.identity;
using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ahis.template.api.Services;

public static class FirstSuperadminBootstrapCli
{
    private const string Command = "bootstrap-superadmin";
    private const string TargetAddress = "tcp:172.20.198.156";
    private const string TargetDatabase = "AhisApiTemplateV2Db";

    public static bool IsCommand(string[] args) => args.Length > 0 && args[0] == Command;

    public static async Task<int> RunAsync(string[] args, string contentRoot, CancellationToken cancellationToken = default)
    {
        if (!TryParse(args, out var userId) || !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")) ||
            Environment.GetEnvironmentVariables().Keys.Cast<string>().Any(k => k.Equals("ConnectionStrings__DefaultConnection", StringComparison.OrdinalIgnoreCase) || k.Equals("ConnectionStrings:DefaultConnection", StringComparison.OrdinalIgnoreCase))) return 1;
        if (!TryReadFixedConnectionString(contentRoot, out var connectionString) || !TryValidateConnectionString(connectionString, out _)) return 1;

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            if (!await AttestAsync(connection, cancellationToken)) return 1;
            var services = new ServiceCollection();
            services.AddIdentityServices(connectionString);
            services.AddIdentityCore<ahis.template.identity.Models.Entities.ApplicationUser>().AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>().AddEntityFrameworkStores<IdentityContext>();
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
            context.Database.SetDbConnection(connection, contextOwnsConnection: false);
            var service = scope.ServiceProvider.GetRequiredService<IFirstSuperadminBootstrapService>();
            return await service.BootstrapAsync(userId!, cancellationToken) ? 0 : 1;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return 1; }
    }

    internal static bool TryParse(string[] args, out string? userId)
    {
        userId = null;
        if (args.Length != 4 || args[0] != Command || args[1] != "--user-id" || args[3] != "--confirm-bootstrap" || string.IsNullOrWhiteSpace(args[2])) return false;
        userId = args[2]; return true;
    }

    internal static bool TryReadFixedConnectionString(string contentRoot, out string value)
    {
        value = string.Empty;
        var path = Path.Combine(contentRoot, "appsettings.Development.json");
        if (!File.Exists(path)) return false;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("ConnectionStrings", out var section) || section.ValueKind != JsonValueKind.Object ||
                !section.TryGetProperty("DefaultConnection", out var item) || item.ValueKind != JsonValueKind.String) return false;
            value = item.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (JsonException) { return false; }
    }

    internal static bool TryValidateConnectionString(string value, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            // Reject duplicate endpoint/database forms before SqlClient's last-value-wins parsing.
            var pairs = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var keys = pairs.Select(x => x.Split('=', 2)[0].Trim().ToLowerInvariant()).ToList();
            if (pairs.Any(x => x.Split('=', 2).Length != 2) || keys.Count(k => k is "server" or "data source" or "addr" or "address" or "network address") != 1 ||
                keys.Count(k => k is "database" or "initial catalog") != 1) return false;
            var builder = new SqlConnectionStringBuilder(value);
            if (!string.Equals(builder.DataSource, TargetAddress, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(builder.InitialCatalog, TargetDatabase, StringComparison.Ordinal) ||
                builder.DataSource.Any(char.IsWhiteSpace) || builder.DataSource.Contains(',') || builder.DataSource.Contains('\\') ||
                builder.ContainsKey("Failover Partner") || builder.ContainsKey("Application Intent") || builder.ContainsKey("Multi Subnet Failover") ||
                builder.ContainsKey("Network Library") || builder.ContainsKey("AttachDBFilename") || builder.ContainsKey("User Instance")) return false;
            normalized = builder.ConnectionString;
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private static async Task<bool> AttestAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT net_transport, local_net_address, local_tcp_port, DB_NAME() FROM sys.dm_exec_connections WHERE session_id = @@SPID";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) && !reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(2) && !reader.IsDBNull(3) &&
            string.Equals(reader.GetString(0), "TCP", StringComparison.OrdinalIgnoreCase) && string.Equals(reader.GetString(1), "172.20.198.156", StringComparison.Ordinal) &&
            reader.GetInt32(2) == 1433 && string.Equals(reader.GetString(3), TargetDatabase, StringComparison.Ordinal);
    }
}
