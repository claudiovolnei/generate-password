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

builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PasswordManager")));
builder.Services.AddScoped<IPasswordRepository, SqlPasswordRepository>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IPasswordGenerator, PasswordGenerator>();

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

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("App");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/login", (LoginRequest request, JwtTokenService tokenService) =>
{
    if (!UsersStore.IsValidCredential(request.Username, request.Password))
    {
        return Results.Unauthorized();
    }

    var token = tokenService.Generate(request.Username);
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
