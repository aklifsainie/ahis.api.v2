using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Common
{

    /// <summary>
    /// Marker interface. Only entities implementing this are picked up by the
    /// audit interceptor. Apply this deliberately - not every entity needs an
    /// audit trail (e.g. lookup/reference tables usually don't).
    /// </summary>
    /// 
    public interface IAuditableEntity
    {
        // Every entity that wants auditing must expose a string-representable Id.
        // If your entities already have Guid/int Id from a base entity, that's fine -
        // the interceptor reads it via reflection, this interface is just a tag.
    }

    /// <summary>
    /// Marks a property as sensitive so the audit interceptor masks it instead of
    /// writing the real value into OldValues/NewValues JSON. Critical for fields
    /// like IC number, even though the DB column itself is already AES-encrypted -
    /// we do not want plaintext values leaking into the audit table.
    /// </summary>
    /// 
    [AttributeUsage(AttributeTargets.Property)]
    public class SensitiveDataAttribute : Attribute
    {
    }
}
