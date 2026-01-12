using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.domain.Enums
{
    /// <summary>
    /// Supported two-factor authentication providers
    /// </summary>
    public enum TwoFactorProviderEnum
    {
        /// <summary>
        /// Authenticator app (TOTP)
        /// </summary>
        Authenticator = 1,

        /// <summary>
        /// One-time recovery code
        /// </summary>
        RecoveryCode = 2
    }
}
