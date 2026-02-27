namespace PasswordManager.Api.Models;

public class PasswordEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Description { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
