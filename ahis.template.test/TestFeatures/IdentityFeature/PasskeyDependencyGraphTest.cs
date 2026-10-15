using ahis.template.application.Features.AccountFeatures.Commands;
using ahis.template.application.Features.AuthenticationFeatures.Commands;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Services;

namespace ahis.template.test.TestFeatures.IdentityFeature;

public sealed class PasskeyDependencyGraphTest
{
    [Fact]
    public void PasskeyHandlersDependDirectlyOnPasskeyServiceWithoutReverseServiceDependencies()
    {
        var authenticationConstructor = typeof(AuthenticationService).GetConstructors().Single();
        var accountConstructor = typeof(AccountService).GetConstructors().Single();
        var accountHandlerConstructor = typeof(PasskeyAccountHandler).GetConstructors().Single();
        var authenticationHandlerConstructor = typeof(PasskeyAuthenticationHandler).GetConstructors().Single();

        Assert.DoesNotContain(authenticationConstructor.GetParameters(), parameter => parameter.ParameterType == typeof(IPasskeyService));
        Assert.DoesNotContain(accountConstructor.GetParameters(), parameter => parameter.ParameterType == typeof(IPasskeyService));
        Assert.Contains(accountHandlerConstructor.GetParameters(), parameter => parameter.ParameterType == typeof(IPasskeyService));
        Assert.Contains(authenticationHandlerConstructor.GetParameters(), parameter => parameter.ParameterType == typeof(IPasskeyService));
    }
}
