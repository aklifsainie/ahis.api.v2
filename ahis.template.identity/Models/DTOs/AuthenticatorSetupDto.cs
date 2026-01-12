using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.identity.Models.DTOs
{
    public class AuthenticatorSetupDto
    {
        public string? Key { get; set; }
        public string? ProvisionUri { get; set; }
    }
}
