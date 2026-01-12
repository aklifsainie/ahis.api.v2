using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.ViewModels.AuthenticationVM
{
    public class CheckAccountStateVM
    {
        public bool RequiresTwoFactor { get; init; }
        public bool IsEmailConfirmed { get; init; }
        public bool IsPasswordCreated { get; init; }
    }
}
