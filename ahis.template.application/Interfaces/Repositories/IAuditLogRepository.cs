using ahis.template.domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Interfaces.Repositories
{
    public interface IAuditLogRepository : IGenericGuidRepository<AuditLog>
    {
    }
}
