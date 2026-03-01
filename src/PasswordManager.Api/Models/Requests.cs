namespace PasswordManager.Api.Models;

public record LoginRequest(string Username, string Password, bool MobileAuthenticationConfirmed = false);

public record RegisterUserRequest(string Username, string Password, bool RequireMobileAuthentication = true);

public record CreatePasswordRequest(string Description, string Username, string? Password);

public record GeneratePasswordRequest(int Length = 16, bool IncludeUppercase = true, bool IncludeLowercase = true, bool IncludeNumbers = true, bool IncludeSymbols = true);
