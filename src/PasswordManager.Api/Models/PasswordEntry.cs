namespace PasswordManager.Api.Models;

public class PasswordEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserAccountId { get; set; }
    public UserAccount? UserAccount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
