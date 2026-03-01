using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace PasswordManager.App.Services;

public sealed class DeviceAuthService
{
    public async Task<bool> AuthenticateAsync(string title = "Validação biométrica", string reason = "Use sua digital ou reconhecimento facial para acessar")
    {
#if ANDROID || IOS || MACCATALYST
        var isAvailable = await CrossFingerprint.Current.IsAvailableAsync(true);
        if (!isAvailable)
        {
            return false;
        }

        var request = new AuthenticationRequestConfiguration(title, reason)
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
