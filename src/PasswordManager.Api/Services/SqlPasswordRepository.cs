using PasswordManager.Api.Infrastructure;
using PasswordManager.Api.Models;
using PasswordManager.Api.Security;

namespace PasswordManager.Api.Services;

public class SqlPasswordRepository(PasswordManagerDbContext dbContext, SecretMaskingService maskingService) : IPasswordRepository
{
    public IReadOnlyCollection<PasswordEntry> GetAll(Guid userAccountId) =>
        dbContext.PasswordEntries
            .Where(item => item.UserAccountId == userAccountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .AsEnumerable()
            .Select(entry => new PasswordEntry
            {
                Id = entry.Id,
                UserAccountId = entry.UserAccountId,
                Description = entry.Description,
                Username = entry.Username,
                Secret = maskingService.Unmask(entry.Secret),
                CreatedAtUtc = entry.CreatedAtUtc
            })
            .ToList();

    public PasswordEntry Add(PasswordEntry entry)
    {
        var dbEntry = new PasswordEntry
        {
            Id = entry.Id,
            UserAccountId = entry.UserAccountId,
            Description = entry.Description,
            Username = entry.Username,
            Secret = maskingService.Mask(entry.Secret),
            CreatedAtUtc = entry.CreatedAtUtc
        };

        dbContext.PasswordEntries.Add(dbEntry);
        dbContext.SaveChanges();

        return entry;
    }

    public bool Delete(Guid id, Guid userAccountId)
    {
        var entry = dbContext.PasswordEntries.FirstOrDefault(item => item.Id == id && item.UserAccountId == userAccountId);
        if (entry is null)
        {
            return false;
        }

        dbContext.PasswordEntries.Remove(entry);
        dbContext.SaveChanges();
        return true;
    }
}
