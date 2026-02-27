using PasswordManager.Api.Models;

namespace PasswordManager.Api.Services;

public interface IPasswordRepository
{
    IReadOnlyCollection<PasswordEntry> GetAll();
    PasswordEntry Add(PasswordEntry entry);
    bool Delete(Guid id);
}
