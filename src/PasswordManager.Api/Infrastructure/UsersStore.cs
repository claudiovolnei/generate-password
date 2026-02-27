using PasswordManager.Api.Models;

namespace PasswordManager.Api.Infrastructure;

public static class UsersStore
{
    private static readonly List<UserAccount> Users =
    [
        new("admin", "Admin@123"),
        new("gestor", "Gestor@123")
    ];

    private static readonly object Sync = new();

    public static bool IsValidCredential(string username, string password) =>
        Users.Any(user =>
            user.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            user.Password == password);

    public static bool AddUser(string username, string password)
    {
        lock (Sync)
        {
            if (Users.Any(user => user.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            Users.Add(new UserAccount(username, password));
            return true;
        }
    }
}
