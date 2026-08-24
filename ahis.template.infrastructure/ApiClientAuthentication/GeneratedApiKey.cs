using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.ApiClientAuthentication
{
    public sealed record GeneratedApiKey(string RawKey, string KeyPrefix, string KeyHash);
}
