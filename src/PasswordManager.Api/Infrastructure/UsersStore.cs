using PasswordManager.Api.Models;

namespace PasswordManager.Api.Infrastructure;

public static class UsersStore
{
    private static readonly IReadOnlyCollection<UserAccount> Users =
    [
        new("admin", "Admin@123"),
        new("gestor", "Gestor@123")
    ];

    public static bool IsValidCredential(string username, string password) =>
        Users.Any(user =>
            user.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            user.Password == password);
}
