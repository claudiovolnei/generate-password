using PasswordManager.Api.Infrastructure;
using PasswordManager.Api.Models;

namespace PasswordManager.Api.Services;

public class SqlPasswordRepository(PasswordManagerDbContext dbContext) : IPasswordRepository
{
    public IReadOnlyCollection<PasswordEntry> GetAll() =>
        dbContext.PasswordEntries
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToList();

    public PasswordEntry Add(PasswordEntry entry)
    {
        dbContext.PasswordEntries.Add(entry);
        dbContext.SaveChanges();
        return entry;
    }
}
