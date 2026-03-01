namespace PasswordManager.Api.Models;

public class UserAccount
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RequireMobileAuthentication { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
