using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace PasswordManager.App.Services;

public sealed class DeviceAuthService
{
    public async Task<bool> AuthenticateAsync()
    {
#if ANDROID || IOS || MACCATALYST
        var isAvailable = await CrossFingerprint.Current.IsAvailableAsync(true);
        if (!isAvailable)
        {
            return false;
        }

        var request = new AuthenticationRequestConfiguration("Validação biométrica", "Use sua digital para acessar o cofre")
        {
            CancelTitle = "Cancelar",
            AllowAlternativeAuthentication = true
        };

        var result = await CrossFingerprint.Current.AuthenticateAsync(request);
        return result.Authenticated;
#else
        return true;
#endif
    }
}
