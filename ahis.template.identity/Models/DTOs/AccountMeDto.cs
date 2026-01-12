using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.identity.Models.DTOs
{
    public class AccountMeDto
    {
        public string UserId { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string Username { get; init; } = default!;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public bool EmailConfirmed { get; init; }
        public bool TwoFactorEnabled { get; init; }
    }
}
