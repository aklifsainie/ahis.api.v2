using System.Data.Common;
using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ahis.template.identity.Services;

/// <summary>Coordinates effective-Superadmin writers on the current SQL Server transaction.</summary>
public sealed class EffectiveSuperadminLock : IEffectiveSuperadminLock
{
    private readonly IdentityContext _context;

    public EffectiveSuperadminLock(IdentityContext context) => _context = context;

    public async Task<bool> AcquireAsync(CancellationToken cancellationToken)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction is null || !_context.Database.IsSqlServer())
            return false;

        try
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = N'ahis:effective-superadmin', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 1000; SELECT @result;";
            command.CommandTimeout = 3;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null and not DBNull && Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) >= 0;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
