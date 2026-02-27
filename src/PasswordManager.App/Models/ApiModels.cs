namespace PasswordManager.App.Models;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token);

public record PasswordEntry(Guid Id, string Description, string Username, string Secret, DateTime CreatedAtUtc);
public record CreatePasswordRequest(string Description, string Username, string? Password);

public record GeneratePasswordRequest(int Length = 16, bool IncludeUppercase = true, bool IncludeLowercase = true, bool IncludeNumbers = true, bool IncludeSymbols = true);
public record GeneratePasswordResponse(string Password);
