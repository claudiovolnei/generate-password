using PasswordManager.Api.Models;

namespace PasswordManager.Api.Services;

public interface IPasswordGenerator
{
    string Generate(GeneratePasswordRequest request);
}
