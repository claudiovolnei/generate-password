using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PasswordManager.Api.Infrastructure;
using PasswordManager.Api.Models;
using PasswordManager.Api.Security;
using PasswordManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

var swaggerUsername = builder.Configuration["SwaggerAuth:Username"] ?? "swagger";
var swaggerPassword = builder.Configuration["SwaggerAuth:Password"] ?? "Swagger@123";

builder.Services.AddDataProtection();
builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PasswordManager")));
builder.Services.AddScoped<IPasswordRepository, SqlPasswordRepository>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
builder.Services.AddSingleton<PasswordHasherService>();
builder.Services.AddScoped<SecretMaskingService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Password Manager API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Informe o token JWT no formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("App", policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManagerDbContext>();
    dbContext.Database.Migrate();
}

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        await next();
        return;
    }

    var header = context.Request.Headers.Authorization.ToString();
    if (AuthenticationHeaderValue.TryParse(header, out var authHeader) &&
        authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(authHeader.Parameter))
    {
        try
        {
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length == 2 &&
                credentials[0] == swaggerUsername &&
                credentials[1] == swaggerPassword)
            {
                await next();
                return;
            }
        }
        catch (FormatException)
        {
        }
    }

    context.Response.Headers.WWWAuthenticate = "Basic realm=\"Swagger\"";
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
});

app.UseSwagger();
app.UseStaticFiles();
app.UseSwaggerUI(options =>
{
    options.InjectJavascript("/swagger-custom.js");
});
app.UseCors("App");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", async (RegisterUserRequest request, PasswordManagerDbContext dbContext, PasswordHasherService hasher) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Usuário e senha são obrigatórios." });
    }

    var username = request.Username.Trim();
    var userExists = await dbContext.UserAccounts
        .AnyAsync(user => user.Username == username);
    if (userExists)
    {
        return Results.Conflict(new { message = "Usuário já existe." });
    }

    var userAccount = new UserAccount
    {
        Id = Guid.NewGuid(),
        Username = username,
        Password = hasher.Hash(request.Password),
        CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.UserAccounts.Add(userAccount);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/auth/users/{userAccount.Username}", new { username = userAccount.Username });
})
.AllowAnonymous()
.WithTags("Auth");

app.MapPost("/api/auth/login", async (LoginRequest request, JwtTokenService tokenService, PasswordManagerDbContext dbContext, PasswordHasherService hasher) =>
{
    var user = await dbContext.UserAccounts.FirstOrDefaultAsync(user => user.Username == request.Username);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var isValid = user.Password.Contains('.')
        ? hasher.Verify(request.Password, user.Password)
        : user.Password == request.Password;

    if (!isValid)
    {
        return Results.Unauthorized();
    }

    if (!user.Password.Contains('.'))
    {
        user.Password = hasher.Hash(request.Password);
        await dbContext.SaveChangesAsync();
    }

    var token = tokenService.Generate(user.Username);
    return Results.Ok(new { token });
})
.AllowAnonymous()
.WithTags("Auth");

var passwords = app.MapGroup("/api/passwords")
    .WithTags("Passwords")
    .RequireAuthorization();

passwords.MapGet("/", (IPasswordRepository repository) => Results.Ok(repository.GetAll()));

passwords.MapPost("/generate", (GeneratePasswordRequest request, IPasswordGenerator generator) =>
{
    var generated = generator.Generate(request);
    return Results.Ok(new { password = generated });
});

passwords.MapPost("/", (CreatePasswordRequest request, IPasswordRepository repository, IPasswordGenerator generator) =>
{
    var secret = string.IsNullOrWhiteSpace(request.Password)
        ? generator.Generate(new GeneratePasswordRequest())
        : request.Password;

    var entry = repository.Add(new PasswordEntry
    {
        Description = request.Description,
        Username = request.Username,
        Secret = secret,
        CreatedAtUtc = DateTime.UtcNow
    });

    return Results.Created($"/api/passwords/{entry.Id}", entry);
});

app.Run();
