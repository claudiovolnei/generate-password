namespace PasswordManager.App.Services;

public class AuthState
{
    public string? Token { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public void SetToken(string token) => Token = token;

    public void Logout() => Token = null;
}
