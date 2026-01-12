using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.ViewModels.AccountVM
{
    public class AuthenticatorSetupResponseVM
    {
        public string? Key { get; set; }
        public string? ProvisionUri { get; set; }
    }
}
