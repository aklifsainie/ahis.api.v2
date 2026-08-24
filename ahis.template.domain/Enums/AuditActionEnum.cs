using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Enums
{
    public enum AuditActionEnum
    {
        Create = 1,
        Update = 2,
        Delete = 3,
        View = 4,
        Login = 5,
        LoginFailed = 6,
        Logout = 7,
        Export = 8,
        StatusChange = 9
    }
}
