using System.Collections.Concurrent;
using PasswordManager.Api.Models;

namespace PasswordManager.Api.Services;

public class InMemoryPasswordRepository : IPasswordRepository
{
    private readonly ConcurrentDictionary<Guid, PasswordEntry> _entries = new();

    public InMemoryPasswordRepository()
    {
        var seed = new PasswordEntry
        {
            UserAccountId = Guid.Empty,
            Description = "Conta de exemplo",
            Username = "admin@empresa.com",
            Secret = "Exemplo@1234"
        };
        _entries.TryAdd(seed.Id, seed);
    }

    public IReadOnlyCollection<PasswordEntry> GetAll(Guid userAccountId) =>
        _entries.Values
            .Where(item => item.UserAccountId == userAccountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToList();

    public PasswordEntry Add(PasswordEntry entry)
    {
        _entries.TryAdd(entry.Id, entry);
        return entry;
    }

    public bool Delete(Guid id, Guid userAccountId)
    {
        if (!_entries.TryGetValue(id, out var entry) || entry.UserAccountId != userAccountId)
        {
            return false;
        }

        return _entries.TryRemove(id, out _);
    }
}
