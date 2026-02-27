using System.Security.Cryptography;
using PasswordManager.Api.Models;

namespace PasswordManager.Api.Services;

public class PasswordGenerator : IPasswordGenerator
{
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnpqrstuvwxyz";
    private const string Numbers = "23456789";
    private const string Symbols = "!@#$%&*_-+=?";

    public string Generate(GeneratePasswordRequest request)
    {
        var pool = BuildPool(request);

        if (request.Length is < 8 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Length), "A senha deve ter entre 8 e 64 caracteres.");
        }

        if (pool.Length == 0)
        {
            throw new InvalidOperationException("Selecione ao menos um grupo de caracteres para gerar a senha.");
        }

        Span<char> chars = stackalloc char[request.Length];

        for (var i = 0; i < chars.Length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(pool.Length);
            chars[i] = pool[index];
        }

        return new string(chars);
    }

    private static string BuildPool(GeneratePasswordRequest request)
    {
        var pool = string.Empty;
        if (request.IncludeUppercase) pool += Uppercase;
        if (request.IncludeLowercase) pool += Lowercase;
        if (request.IncludeNumbers) pool += Numbers;
        if (request.IncludeSymbols) pool += Symbols;

        return pool;
    }
}
