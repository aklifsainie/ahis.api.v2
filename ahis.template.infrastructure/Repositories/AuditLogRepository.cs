using ahis.template.application.Interfaces.Repositories;
using ahis.template.domain.Models.Entities;
using ahis.template.infrastructure.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.Repositories
{
    public class AuditLogRepository : GenericGuidRepository<AuditLog>, IAuditLogRepository
    {

        public AuditLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
