using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PasswordManager.Api.Infrastructure;
using PasswordManager.Api.Models;
using PasswordManager.Api.Security;
using PasswordManager.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var seqServerUrl = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqServerUrl))
    {
        loggerConfiguration.WriteTo.Seq(
            seqServerUrl,
            apiKey: context.Configuration["Seq:ApiKey"]);
    }
});

builder.Services.AddHttpClient("SeqProxy");
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

app.UseSerilogRequestLogging();

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

app.Map("/seq/{**path}", async (HttpContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
{
    var seqServerUrl = configuration["Seq:ServerUrl"];
    if (string.IsNullOrWhiteSpace(seqServerUrl))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Seq não configurado. Defina Seq:ServerUrl.");
        return;
    }

    var path = context.Request.RouteValues["path"]?.ToString() ?? string.Empty;
    var targetUri = new Uri($"{seqServerUrl.TrimEnd('/')}/{path}{context.Request.QueryString}");

    using var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

    foreach (var header in context.Request.Headers)
    {
        if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            requestMessage.Content ??= new StreamContent(context.Request.Body);
            requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        requestMessage.Content ??= new StreamContent(context.Request.Body);
    }

    var client = httpClientFactory.CreateClient("SeqProxy");
    using var responseMessage = await client.SendAsync(
        requestMessage,
        HttpCompletionOption.ResponseHeadersRead,
        context.RequestAborted);

    context.Response.StatusCode = (int)responseMessage.StatusCode;

    foreach (var header in responseMessage.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    foreach (var header in responseMessage.Content.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    context.Response.Headers.Remove("transfer-encoding");
    await responseMessage.Content.CopyToAsync(context.Response.Body);
});

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
        RequireMobileAuthentication = request.RequireMobileAuthentication,
        CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.UserAccounts.Add(userAccount);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/auth/users/{userAccount.Username}", new
    {
        username = userAccount.Username,
        requireMobileAuthentication = userAccount.RequireMobileAuthentication
    });
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

    if (user.RequireMobileAuthentication && !request.MobileAuthenticationConfirmed)
    {
        return Results.StatusCode(StatusCodes.Status428PreconditionRequired);
    }

    var token = tokenService.Generate(user.Id, user.Username);
    return Results.Ok(new { token, requireMobileAuthentication = user.RequireMobileAuthentication });
})
.AllowAnonymous()
.WithTags("Auth");

var passwords = app.MapGroup("/api/passwords")
    .WithTags("Passwords")
    .RequireAuthorization();

passwords.MapGet("/", (ClaimsPrincipal user, IPasswordRepository repository) =>
{
    if (!TryGetUserId(user, out var userId))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(repository.GetAll(userId));
});

passwords.MapPost("/generate", (GeneratePasswordRequest request, IPasswordGenerator generator) =>
{
    var generated = generator.Generate(request);
    return Results.Ok(new { password = generated });
});

passwords.MapPost("/", (ClaimsPrincipal user, CreatePasswordRequest request, IPasswordRepository repository, IPasswordGenerator generator) =>
{
    if (!TryGetUserId(user, out var userId))
    {
        return Results.Unauthorized();
    }

    var secret = string.IsNullOrWhiteSpace(request.Password)
        ? generator.Generate(new GeneratePasswordRequest())
        : request.Password;

    var entry = repository.Add(new PasswordEntry
    {
        UserAccountId = userId,
        Description = request.Description,
        Username = request.Username,
        Secret = secret,
        CreatedAtUtc = DateTime.UtcNow
    });

    return Results.Created($"/api/passwords/{entry.Id}", entry);
});

passwords.MapDelete("/{id:guid}", (ClaimsPrincipal user, Guid id, IPasswordRepository repository) =>
{
    if (!TryGetUserId(user, out var userId))
    {
        return Results.Unauthorized();
    }

    var deleted = repository.Delete(id, userId);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();

static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
{
    var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(claimValue, out userId);
}
