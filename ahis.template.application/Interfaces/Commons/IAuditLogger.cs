using ahis.template.domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Interfaces.Commons
{
    /// <summary>
    /// For audit events that are NOT captured automatically by
    /// AuditSaveChangesInterceptor - i.e. anything that isn't a Create/Update/Delete
    /// going through SaveChanges. Reads (View), Login/Logout, and Exports fall here.
    ///
    /// Call this explicitly from query/command handlers. It writes directly and
    /// commits immediately - it does not wait for a business-data SaveChanges call.
    /// </summary>
    public interface IAuditLogger
    {

        /// <param name="overrideUserId">
        /// Use for events where the actor is NOT the currently-authenticated user
        /// (there isn't one yet) - e.g. login attempts. When null, falls back to
        /// ICurrentUserService.UserId. IP/UserAgent/CorrelationId always come from
        /// the current HTTP request regardless, since those exist pre-auth too.
        /// </param>
        /// 

        Task LogAsync(
        AuditActionEnum action,
        string entityName,
        string entityId,
        string? metadata = null,
        string? overrideUserId = null,
        string? overrideUserName = null,
        string? overrideUserRole = null,
        CancellationToken cancellationToken = default);
    }
}
