using Microsoft.AspNetCore.DataProtection;

namespace PasswordManager.Api.Security;

public class SecretMaskingService(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("PasswordManager.Api.SecretMasking");

    public string Mask(string secret) => _protector.Protect(secret);

    public string Unmask(string maskedSecret)
    {
        try
        {
            return _protector.Unprotect(maskedSecret);
        }
        catch
        {
            return maskedSecret;
        }
    }
}
