using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Models.ViewModels.AuthenticationVM
{
    public class AuthenticationResponseVM
    {
        public string AccessToken { get; init; } = string.Empty;
        public int ExpiresInSeconds { get; init; }
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; init; }
        public string UserId { get; init; } = string.Empty;

        public bool RequiresTwoFactor { get; init; }
        public bool IsEmailConfirmed { get; init; }
        public bool IsPasswordCreated { get; init; }
    }
}
