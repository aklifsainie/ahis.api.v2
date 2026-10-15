using ahis.template.identity.Models.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ahis.template.identity.Services;

public sealed class InitialPasswordSetupTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public InitialPasswordSetupTokenProviderOptions()
    {
        Name = "InitialPasswordSetupTokenProvider";
        TokenLifespan = TimeSpan.FromMinutes(30);
    }
}

public sealed class InitialPasswordSetupTokenProvider : DataProtectorTokenProvider<ApplicationUser>
{
    public const string ProviderName = "InitialPasswordSetup";
    public const string Purpose = "InitialPasswordSetup";

    public InitialPasswordSetupTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<InitialPasswordSetupTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
        : base(
            dataProtectionProvider,
            Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions
            {
                Name = options.Value.Name,
                TokenLifespan = options.Value.TokenLifespan
            }),
            logger)
    {
    }
}
