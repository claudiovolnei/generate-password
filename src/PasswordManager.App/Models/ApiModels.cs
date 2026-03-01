namespace PasswordManager.App.Models;

public record LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool MobileAuthenticationConfirmed { get; set; }
}

public record LoginResponse(string Token, bool RequireMobileAuthentication);

public record LoginResult(bool IsSuccess, bool RequiresMobileAuthentication, string? ErrorMessage = null);

public record RegisterUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RequireMobileAuthentication { get; set; } = true;
}

public record PasswordEntry(Guid Id, string Description, string Username, string Secret, DateTime CreatedAtUtc);

public record CreatePasswordRequest
{
    public string Description { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public record GeneratePasswordRequest(int Length = 16, bool IncludeUppercase = true, bool IncludeLowercase = true, bool IncludeNumbers = true, bool IncludeSymbols = true);
public record GeneratePasswordResponse(string Password);
