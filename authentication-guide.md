# Authentication Implementation Guide

A complete guide for implementing authentication in a C# API with React UI and PostgreSQL, supporting both local accounts and OIDC providers (like Authentik).

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Phase 1: Project Setup](#phase-1-project-setup)
- [Phase 2: Database & Identity](#phase-2-database--identity)
- [Phase 3: JWT Authentication](#phase-3-jwt-authentication)
- [Phase 4: Auth Endpoints](#phase-4-auth-endpoints)
- [Phase 5: Docker Configuration](#phase-5-docker-configuration)
- [Phase 6: OIDC Support](#phase-6-oidc-support)
- [Phase 7: React Frontend with TanStack Router & Axios](#phase-7-react-frontend-with-tanstack-router--axios)
- [Phase 8: Refresh Tokens & Server-side Logout](#phase-8-refresh-tokens--server-side-logout)
- [Phase 9: Optional Authentication Bypass](#phase-9-optional-authentication-bypass)
- [Phase 10: Testing](#phase-10-testing)
- [Appendix: UI Settings for OIDC](#appendix-ui-settings-for-oidc)

---

## Overview

### Features

- ✅ Local user accounts with ASP.NET Core Identity
- ✅ JWT token authentication
- ✅ Refresh token rotation with server-side storage
- ✅ Server-side logout (token revocation)
- ✅ Password reset (admin-triggered, logs temp password)
- ✅ User management (list, update roles, delete)
- ✅ Profile update (users can change display name & email)
- ✅ Role-based authorization (Admin, Reader)
- ✅ Default admin user created on first startup (`admin:admin`)
- ✅ OIDC support for external providers (Authentik, Keycloak, etc.)
- ✅ Account linking (connect OIDC to existing local account)
- ✅ Auto-provisioning of users from OIDC
- ✅ Auto token refresh in React (transparent to user)
- ✅ Protected routes with TanStack Router
- ✅ Optional auth bypass for development
- ✅ Docker containerization

### Tech Stack

- **Backend**: C# .NET 8/9, ASP.NET Core Identity, Entity Framework Core
- **Database**: PostgreSQL
- **Frontend**: React with TypeScript
- **Auth**: JWT + OIDC
- **Infrastructure**: Docker, Docker Compose

---

## Architecture

```
┌─────────────┐     JWT      ┌─────────────┐     EF Core    ┌────────────┐
│   React UI  │ ◄──────────► │   C# API    │ ◄────────────► │ PostgreSQL │
└─────────────┘              └─────────────┘                └────────────┘
       │                            │
       │                      ┌─────┴─────┐
       │                      │           │
       │                   Local      OIDC
       │                  Accounts   Providers
       │                      │           │
       │                      └─────┬─────┘
       │                            │
       └────── OIDC Flow ──────────►│
                                    ▼
                            ┌─────────────┐
                            │  Authentik  │
                            │  Keycloak   │
                            │    etc.     │
                            └─────────────┘
```

### Project Structure

```
src/
├── Api/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── OidcController.cs
│   │   ├── DataController.cs
│   │   └── Admin/
│   │       └── OidcProvidersController.cs
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── DbSeeder.cs
│   ├── Models/
│   │   ├── ApplicationUser.cs
│   │   ├── ExternalLogin.cs
│   │   ├── OidcProvider.cs
│   │   └── Auth/
│   │       ├── LoginRequest.cs
│   │       ├── LoginResponse.cs
│   │       ├── RegisterRequest.cs
│   │       └── OidcModels.cs
│   ├── Services/
│   │   ├── ITokenService.cs
│   │   ├── TokenService.cs
│   │   ├── IOidcService.cs
│   │   └── OidcService.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── frontend/
│   ├── src/
│   │   ├── hooks/
│   │   │   └── useAuth.ts
│   │   ├── components/
│   │   │   └── LoginForm.tsx
│   │   └── ...
│   └── Dockerfile
└── docker-compose.yml
```

---

## Phase 1: Project Setup

### Step 1.1: Create the API Project

```bash
# Create solution and project
mkdir src && cd src
dotnet new webapi -n Api -o Api
cd Api

# Add required packages
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### Step 1.2: Create the React Frontend

```bash
cd ../
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install @tanstack/react-router axios zustand
npm install -D @tanstack/router-plugin
```

---

## Phase 2: Database & Identity

### Step 2.1: Create the ApplicationUser Model

Create `Models/ApplicationUser.cs`:

```csharp
using Microsoft.AspNetCore.Identity;

namespace Api.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
```

### Step 2.2: Create the Database Context

Create `Data/ApplicationDbContext.cs`:

```csharp
using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("auth");
    }
}
```

### Step 2.3: Create the Database Seeder

Create `Data/DbSeeder.cs`:

```csharp
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Data;

public static class DbSeeder
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Reader = "Reader";
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Apply migrations
        await context.Database.MigrateAsync();

        // Seed roles
        await SeedRolesAsync(roleManager, logger);

        // Seed admin user
        await SeedAdminUserAsync(userManager, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roles = [Roles.Admin, Roles.Reader];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminUsername = "admin";
        const string adminPassword = "admin"; // Change in production!

        var adminUser = await userManager.FindByNameAsync(adminUsername);
        
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminUsername,
                Email = "admin@localhost",
                DisplayName = "Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                logger.LogWarning(
                    "Created default admin user with username '{Username}' and password '{Password}'. " +
                    "Please change the password immediately!",
                    adminUsername, adminPassword);
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
```

---

## Phase 3: JWT Authentication

### Step 3.1: Create Auth Request/Response Models

Create `Models/Auth/LoginRequest.cs`:

```csharp
namespace Api.Models.Auth;

public record LoginRequest(string Username, string Password);
```

Create `Models/Auth/LoginResponse.cs`:

```csharp
namespace Api.Models.Auth;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User
);

public record UserInfo(
    string Id,
    string Username,
    string? DisplayName,
    IEnumerable<string> Roles
);
```

Create `Models/Auth/RegisterRequest.cs`:

```csharp
namespace Api.Models.Auth;

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? DisplayName
);
```

### Step 3.2: Create the Token Service

Create `Services/ITokenService.cs`:

```csharp
using Api.Models;

namespace Api.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiry();
}
```

Create `Services/TokenService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add roles as claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = GetAccessTokenExpiry();

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public DateTime GetAccessTokenExpiry()
    {
        var minutes = _configuration.GetValue("Jwt:ExpiryMinutes", 60);
        return DateTime.UtcNow.AddMinutes(minutes);
    }
}
```

---

## Phase 4: Auth Endpoints

### Step 4.1: Create the Auth Controller

Create `Controllers/AuthController.cs`:

```csharp
using Api.Data;
using Api.Models;
using Api.Models.Auth;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);
        
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return Unauthorized(new { message = "Account is locked. Try again later." });
            }
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _logger.LogInformation("User {Username} logged in successfully", user.UserName);

        return Ok(new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: _tokenService.GetAccessTokenExpiry(),
            User: new UserInfo(user.Id, user.UserName!, user.DisplayName, roles)
        ));
    }

    [HttpPost("register")]
    [Authorize(Roles = DbSeeder.Roles.Admin)]
    public async Task<ActionResult<UserInfo>> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, DbSeeder.Roles.Reader);
        var roles = await _userManager.GetRolesAsync(user);
        
        _logger.LogInformation("New user {Username} registered by admin", user.UserName);

        return CreatedAtAction(nameof(GetCurrentUser), new UserInfo(
            user.Id, user.UserName!, user.DisplayName, roles
        ));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        
        return Ok(new UserInfo(user.Id, user.UserName!, user.DisplayName, roles));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {Username} changed their password", user.UserName);
        
        return Ok(new { message = "Password changed successfully" });
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
```

### Step 4.2: Create a Sample Protected Controller

Create `Controllers/DataController.cs`:

```csharp
using Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{DbSeeder.Roles.Admin},{DbSeeder.Roles.Reader}")]
    public IActionResult GetData()
    {
        return Ok(new { message = "This data is visible to both Admin and Reader roles" });
    }

    [HttpPost]
    [Authorize(Roles = DbSeeder.Roles.Admin)]
    public IActionResult CreateData([FromBody] object data)
    {
        return Ok(new { message = "Only admins can create data" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = DbSeeder.Roles.Admin)]
    public IActionResult DeleteData(int id)
    {
        return Ok(new { message = $"Only admins can delete data (id: {id})" });
    }
}
```

### Step 4.3: Configure Program.cs

Replace `Program.cs`:

```csharp
using System.Text;
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings - relaxed for development
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
    };
});

// Services
builder.Services.AddScoped<ITokenService, TokenService>();

// Controllers
builder.Services.AddControllers();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            builder.Configuration.GetSection("Cors:Origins").Get<string[]>() 
            ?? ["http://localhost:3000"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed database
await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Step 4.4: Configure appsettings.json

```json
{
  "App": {
    "BaseUrl": "http://localhost:5000",
    "FrontendUrl": "http://localhost:3000"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=myapp;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-that-should-be-at-least-32-characters-long",
    "Issuer": "myapp-api",
    "Audience": "myapp-client",
    "ExpiryMinutes": 60
  },
  "Cors": {
    "Origins": ["http://localhost:3000", "http://localhost:5173"]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## Phase 5: Docker Configuration

### Step 5.1: Create API Dockerfile

Create `src/Api/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
```

### Step 5.2: Create Frontend Dockerfile

Create `src/frontend/Dockerfile`:

```dockerfile
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

Create `src/frontend/nginx.conf`:

```nginx
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api {
        proxy_pass http://api:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### Step 5.3: Create docker-compose.yml

Create `docker-compose.yml` in the root:

```yaml
services:
  api:
    build:
      context: ./src/Api
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=myapp;Username=postgres;Password=postgres
      - Jwt__Secret=your-super-secret-key-that-should-be-at-least-32-characters-long
      - Jwt__Issuer=myapp-api
      - Jwt__Audience=myapp-client
      - App__BaseUrl=http://localhost:5000
      - App__FrontendUrl=http://localhost:3000
      - Cors__Origins__0=http://localhost:3000
    depends_on:
      postgres:
        condition: service_healthy

  frontend:
    build:
      context: ./src/frontend
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    depends_on:
      - api

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: myapp
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

### Step 5.4: Test the Basic Setup

```bash
# Start all services
docker compose up -d

# Check logs
docker compose logs -f api

# Test login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'

# Use the returned token
curl http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer <your-token>"
```

---

## Phase 6: OIDC Support

### Step 6.1: Add OIDC Models

Create `Models/ExternalLogin.cs`:

```csharp
namespace Api.Models;

public class ExternalLogin
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    
    public string Provider { get; set; } = null!;
    public string ProviderKey { get; set; } = null!;
    public string? ProviderDisplayName { get; set; }
    
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
```

Create `Models/OidcProvider.cs`:

```csharp
namespace Api.Models;

public class OidcProvider
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool Enabled { get; set; } = true;
    
    // OIDC Configuration
    public string Authority { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    
    // Optional overrides
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? UserInfoEndpoint { get; set; }
    
    // Behavior settings
    public bool AutoCreateUsers { get; set; } = true;
    public string DefaultRole { get; set; } = "Reader";
    public bool AutoUpdateClaims { get; set; } = true;
    
    // Claim mappings
    public string UsernameClaim { get; set; } = "preferred_username";
    public string EmailClaim { get; set; } = "email";
    public string DisplayNameClaim { get; set; } = "name";
    public string? RolesClaim { get; set; }
    
    // UI customization
    public string? IconUrl { get; set; }
    public string? ButtonColor { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

Create `Models/Auth/OidcModels.cs`:

```csharp
namespace Api.Models.Auth;

public record OidcProviderInfo(
    string Name,
    string DisplayName,
    string? IconUrl,
    string? ButtonColor,
    string LoginUrl
);

public record OidcCallbackRequest(
    string Code,
    string State,
    string? Error,
    string? ErrorDescription
);

public record LinkExternalLoginRequest(string Provider);
```

### Step 6.2: Update Database Context

Update `Data/ApplicationDbContext.cs`:

```csharp
using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<OidcProvider> OidcProviders => Set<OidcProvider>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.HasDefaultSchema("auth");

        builder.Entity<OidcProvider>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.ClientSecret).IsRequired();
        });

        builder.Entity<ExternalLogin>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

### Step 6.3: Create the OIDC Service

Create `Services/IOidcService.cs`:

```csharp
using Api.Models;
using Api.Models.Auth;

namespace Api.Services;

public interface IOidcService
{
    Task<IEnumerable<OidcProviderInfo>> GetEnabledProvidersAsync();
    Task<string> GetAuthorizationUrlAsync(string providerName, string returnUrl);
    Task<LoginResponse> HandleCallbackAsync(string providerName, string code, string state);
    Task LinkExternalLoginAsync(string userId, string providerName, string code, string state);
    Task UnlinkExternalLoginAsync(string userId, string providerName);
}
```

Create `Services/OidcService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Data;
using Api.Models;
using Api.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Services;

public class OidcService : IOidcService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OidcService> _logger;

    private static readonly Dictionary<string, OidcAuthState> _pendingStates = new();

    public OidcService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<OidcService> logger)
    {
        _context = context;
        _userManager = userManager;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IEnumerable<OidcProviderInfo>> GetEnabledProvidersAsync()
    {
        var providers = await _context.OidcProviders
            .Where(p => p.Enabled)
            .ToListAsync();

        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5000";

        return providers.Select(p => new OidcProviderInfo(
            Name: p.Name,
            DisplayName: p.DisplayName,
            IconUrl: p.IconUrl,
            ButtonColor: p.ButtonColor,
            LoginUrl: $"{baseUrl}/api/auth/oidc/{p.Name}/login"
        ));
    }

    public async Task<string> GetAuthorizationUrlAsync(string providerName, string returnUrl)
    {
        var provider = await GetProviderOrThrowAsync(providerName);
        var discovery = await GetDiscoveryDocumentAsync(provider);

        var state = GenerateSecureToken();
        var nonce = GenerateSecureToken();
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        _pendingStates[state] = new OidcAuthState
        {
            ProviderName = providerName,
            Nonce = nonce,
            CodeVerifier = codeVerifier,
            ReturnUrl = returnUrl,
            CreatedAt = DateTime.UtcNow
        };

        var callbackUrl = $"{_configuration["App:BaseUrl"]}/api/auth/oidc/{providerName}/callback";

        var queryParams = new Dictionary<string, string?>
        {
            ["client_id"] = provider.ClientId,
            ["response_type"] = "code",
            ["scope"] = "openid profile email",
            ["redirect_uri"] = callbackUrl,
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        var authEndpoint = provider.AuthorizationEndpoint ?? discovery.AuthorizationEndpoint;
        return QueryHelpers.AddQueryString(authEndpoint, queryParams);
    }

    public async Task<LoginResponse> HandleCallbackAsync(string providerName, string code, string state)
    {
        if (!_pendingStates.TryGetValue(state, out var authState))
        {
            throw new InvalidOperationException("Invalid or expired state parameter");
        }
        _pendingStates.Remove(state);

        if (DateTime.UtcNow - authState.CreatedAt > TimeSpan.FromMinutes(10))
        {
            throw new InvalidOperationException("Authentication request expired");
        }

        var provider = await GetProviderOrThrowAsync(providerName);
        var discovery = await GetDiscoveryDocumentAsync(provider);

        var tokens = await ExchangeCodeForTokensAsync(provider, discovery, code, authState.CodeVerifier);
        var claims = ParseIdToken(tokens.IdToken, authState.Nonce);

        var user = await GetOrCreateUserAsync(provider, claims);
        await UpdateExternalLoginAsync(user, provider, claims);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        _logger.LogInformation(
            "User {Username} logged in via OIDC provider {Provider}",
            user.UserName, providerName);

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: _tokenService.GenerateRefreshToken(),
            ExpiresAt: _tokenService.GetAccessTokenExpiry(),
            User: new UserInfo(user.Id, user.UserName!, user.DisplayName, roles)
        );
    }

    public async Task LinkExternalLoginAsync(string userId, string providerName, string code, string state)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var existingLink = await _context.ExternalLogins
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == providerName);

        if (existingLink != null)
        {
            throw new InvalidOperationException($"Account already linked to {providerName}");
        }

        if (!_pendingStates.TryGetValue(state, out var authState))
        {
            throw new InvalidOperationException("Invalid or expired state parameter");
        }
        _pendingStates.Remove(state);

        var provider = await GetProviderOrThrowAsync(providerName);
        var discovery = await GetDiscoveryDocumentAsync(provider);
        var tokens = await ExchangeCodeForTokensAsync(provider, discovery, code, authState.CodeVerifier);
        var claims = ParseIdToken(tokens.IdToken, authState.Nonce);

        var providerKey = claims.GetValueOrDefault("sub")
            ?? throw new InvalidOperationException("No subject claim in ID token");

        var existingUser = await _context.ExternalLogins
            .FirstOrDefaultAsync(e => e.Provider == providerName && e.ProviderKey == providerKey);

        if (existingUser != null)
        {
            throw new InvalidOperationException("This external account is already linked to another user");
        }

        _context.ExternalLogins.Add(new ExternalLogin
        {
            UserId = userId,
            Provider = providerName,
            ProviderKey = providerKey,
            ProviderDisplayName = claims.GetValueOrDefault("preferred_username") 
                ?? claims.GetValueOrDefault("email")
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {Username} linked external login from {Provider}",
            user.UserName, providerName);
    }

    public async Task UnlinkExternalLoginAsync(string userId, string providerName)
    {
        var link = await _context.ExternalLogins
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == providerName);

        if (link == null)
        {
            throw new InvalidOperationException($"No linked account found for {providerName}");
        }

        var user = await _userManager.FindByIdAsync(userId)!;
        var hasPassword = await _userManager.HasPasswordAsync(user!);
        var otherLogins = await _context.ExternalLogins
            .CountAsync(e => e.UserId == userId && e.Provider != providerName);

        if (!hasPassword && otherLogins == 0)
        {
            throw new InvalidOperationException(
                "Cannot unlink the only login method. Set a password first.");
        }

        _context.ExternalLogins.Remove(link);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {Username} unlinked external login from {Provider}",
            user!.UserName, providerName);
    }

    #region Private Helpers

    private async Task<OidcProvider> GetProviderOrThrowAsync(string name)
    {
        return await _context.OidcProviders
            .FirstOrDefaultAsync(p => p.Name == name && p.Enabled)
            ?? throw new InvalidOperationException($"OIDC provider '{name}' not found or disabled");
    }

    private async Task<OidcDiscoveryDocument> GetDiscoveryDocumentAsync(OidcProvider provider)
    {
        var cacheKey = $"oidc_discovery_{provider.Name}";

        if (_cache.TryGetValue(cacheKey, out OidcDiscoveryDocument? cached))
        {
            return cached!;
        }

        var client = _httpClientFactory.CreateClient();
        var discoveryUrl = $"{provider.Authority.TrimEnd('/')}/.well-known/openid-configuration";

        var response = await client.GetAsync(discoveryUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<OidcDiscoveryDocument>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        })!;

        _cache.Set(cacheKey, doc, TimeSpan.FromHours(1));
        return doc;
    }

    private async Task<OidcTokenResponse> ExchangeCodeForTokensAsync(
        OidcProvider provider,
        OidcDiscoveryDocument discovery,
        string code,
        string codeVerifier)
    {
        var client = _httpClientFactory.CreateClient();
        var callbackUrl = $"{_configuration["App:BaseUrl"]}/api/auth/oidc/{provider.Name}/callback";

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = callbackUrl,
            ["client_id"] = provider.ClientId,
            ["client_secret"] = provider.ClientSecret,
            ["code_verifier"] = codeVerifier
        });

        var tokenEndpoint = provider.TokenEndpoint ?? discovery.TokenEndpoint;
        var response = await client.PostAsync(tokenEndpoint, tokenRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Token exchange failed: {Error}", error);
            throw new InvalidOperationException("Failed to exchange authorization code");
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OidcTokenResponse>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        })!;
    }

    private Dictionary<string, string> ParseIdToken(string idToken, string expectedNonce)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(idToken);

        var nonce = token.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value;
        if (nonce != expectedNonce)
        {
            throw new InvalidOperationException("Invalid nonce in ID token");
        }

        return token.Claims.ToDictionary(c => c.Type, c => c.Value);
    }

    private async Task<ApplicationUser> GetOrCreateUserAsync(
        OidcProvider provider,
        Dictionary<string, string> claims)
    {
        var providerKey = claims.GetValueOrDefault("sub")
            ?? throw new InvalidOperationException("No subject claim in ID token");

        var existingLogin = await _context.ExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == provider.Name && e.ProviderKey == providerKey);

        if (existingLogin != null)
        {
            var existingUser = existingLogin.User;

            if (provider.AutoUpdateClaims)
            {
                await UpdateUserFromClaimsAsync(existingUser, provider, claims);
            }

            return existingUser;
        }

        var email = claims.GetValueOrDefault(provider.EmailClaim);
        ApplicationUser? user = null;

        if (!string.IsNullOrEmpty(email))
        {
            user = await _userManager.FindByEmailAsync(email);
        }

        if (user == null)
        {
            if (!provider.AutoCreateUsers)
            {
                throw new InvalidOperationException(
                    "No local account found. Please register first or contact an administrator.");
            }

            var username = claims.GetValueOrDefault(provider.UsernameClaim)
                ?? email?.Split('@')[0]
                ?? $"user_{providerKey[..8]}";

            var baseUsername = username;
            var counter = 1;
            while (await _userManager.FindByNameAsync(username) != null)
            {
                username = $"{baseUsername}{counter++}";
            }

            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                DisplayName = claims.GetValueOrDefault(provider.DisplayNameClaim)
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await _userManager.AddToRoleAsync(user, provider.DefaultRole);

            _logger.LogInformation(
                "Created new user {Username} from OIDC provider {Provider}",
                username, provider.Name);
        }

        return user;
    }

    private async Task UpdateUserFromClaimsAsync(
        ApplicationUser user,
        OidcProvider provider,
        Dictionary<string, string> claims)
    {
        var updated = false;

        var displayName = claims.GetValueOrDefault(provider.DisplayNameClaim);
        if (!string.IsNullOrEmpty(displayName) && user.DisplayName != displayName)
        {
            user.DisplayName = displayName;
            updated = true;
        }

        var email = claims.GetValueOrDefault(provider.EmailClaim);
        if (!string.IsNullOrEmpty(email) && user.Email != email)
        {
            user.Email = email;
            updated = true;
        }

        if (!string.IsNullOrEmpty(provider.RolesClaim))
        {
            var rolesJson = claims.GetValueOrDefault(provider.RolesClaim);
            if (!string.IsNullOrEmpty(rolesJson))
            {
                var externalRoles = rolesJson.StartsWith('[')
                    ? JsonSerializer.Deserialize<string[]>(rolesJson)
                    : [rolesJson];

                if (externalRoles != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var rolesToAdd = externalRoles.Except(currentRoles);
                    var rolesToRemove = currentRoles.Except(externalRoles);

                    foreach (var role in rolesToAdd)
                    {
                        if (await _context.Roles.AnyAsync(r => r.Name == role))
                        {
                            await _userManager.AddToRoleAsync(user, role);
                        }
                    }

                    foreach (var role in rolesToRemove)
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }
                }
            }
        }

        if (updated)
        {
            await _userManager.UpdateAsync(user);
        }
    }

    private async Task UpdateExternalLoginAsync(
        ApplicationUser user,
        OidcProvider provider,
        Dictionary<string, string> claims)
    {
        var providerKey = claims["sub"];

        var login = await _context.ExternalLogins
            .FirstOrDefaultAsync(e => e.Provider == provider.Name && e.ProviderKey == providerKey);

        if (login == null)
        {
            login = new ExternalLogin
            {
                UserId = user.Id,
                Provider = provider.Name,
                ProviderKey = providerKey
            };
            _context.ExternalLogins.Add(login);
        }

        login.LastUsedAt = DateTime.UtcNow;
        login.ProviderDisplayName = claims.GetValueOrDefault(provider.UsernameClaim)
            ?? claims.GetValueOrDefault(provider.EmailClaim);

        await _context.SaveChangesAsync();
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    #endregion
}

internal class OidcAuthState
{
    public string ProviderName { get; set; } = null!;
    public string Nonce { get; set; } = null!;
    public string CodeVerifier { get; set; } = null!;
    public string? ReturnUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal class OidcDiscoveryDocument
{
    public string AuthorizationEndpoint { get; set; } = null!;
    public string TokenEndpoint { get; set; } = null!;
    public string UserinfoEndpoint { get; set; } = null!;
    public string JwksUri { get; set; } = null!;
}

internal class OidcTokenResponse
{
    public string AccessToken { get; set; } = null!;
    public string IdToken { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = null!;
}
```

### Step 6.4: Create the OIDC Controller

Create `Controllers/OidcController.cs`:

```csharp
using Api.Models.Auth;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth/oidc")]
public class OidcController : ControllerBase
{
    private readonly IOidcService _oidcService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OidcController> _logger;

    public OidcController(
        IOidcService oidcService,
        IConfiguration configuration,
        ILogger<OidcController> logger)
    {
        _oidcService = oidcService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("providers")]
    public async Task<ActionResult<IEnumerable<OidcProviderInfo>>> GetProviders()
    {
        var providers = await _oidcService.GetEnabledProvidersAsync();
        return Ok(providers);
    }

    [HttpGet("{provider}/login")]
    public async Task<IActionResult> Login(string provider, [FromQuery] string? returnUrl)
    {
        try
        {
            var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:3000";
            var safeReturnUrl = returnUrl ?? frontendUrl;

            if (!Uri.TryCreate(safeReturnUrl, UriKind.Absolute, out _) ||
                !safeReturnUrl.StartsWith(frontendUrl))
            {
                safeReturnUrl = frontendUrl;
            }

            var authUrl = await _oidcService.GetAuthorizationUrlAsync(provider, safeReturnUrl);
            return Redirect(authUrl);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to initiate OIDC login for provider {Provider}", provider);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:3000";

        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning(
                "OIDC callback error from {Provider}: {Error} - {Description}",
                provider, error, errorDescription);

            return Redirect($"{frontendUrl}/login?error={Uri.EscapeDataString(errorDescription ?? error)}");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Redirect($"{frontendUrl}/login?error=Missing authorization code or state");
        }

        try
        {
            var loginResponse = await _oidcService.HandleCallbackAsync(provider, code, state);
            return Redirect($"{frontendUrl}/auth/callback?token={loginResponse.AccessToken}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OIDC callback failed for provider {Provider}", provider);
            return Redirect($"{frontendUrl}/login?error={Uri.EscapeDataString("Authentication failed")}");
        }
    }

    [HttpPost("{provider}/link")]
    [Authorize]
    public async Task<IActionResult> LinkAccount(string provider)
    {
        try
        {
            var returnUrl = $"{_configuration["App:FrontendUrl"]}/settings/accounts";
            var authUrl = await _oidcService.GetAuthorizationUrlAsync(provider, returnUrl);
            return Ok(new { authUrl });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{provider}/link")]
    [Authorize]
    public async Task<IActionResult> UnlinkAccount(string provider)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            await _oidcService.UnlinkExternalLoginAsync(userId, provider);
            return Ok(new { message = $"Unlinked {provider} account" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

### Step 6.5: Create the Admin Controller for Provider Management

Create `Controllers/Admin/OidcProvidersController.cs`:

```csharp
using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers.Admin;

[ApiController]
[Route("api/admin/oidc-providers")]
[Authorize(Roles = DbSeeder.Roles.Admin)]
public class OidcProvidersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OidcProvidersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OidcProviderDto>>> GetAll()
    {
        var providers = await _context.OidcProviders
            .Select(p => new OidcProviderDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Authority = p.Authority,
                ClientId = p.ClientId,
                Enabled = p.Enabled,
                AutoCreateUsers = p.AutoCreateUsers,
                DefaultRole = p.DefaultRole,
                IconUrl = p.IconUrl,
                ButtonColor = p.ButtonColor
            })
            .ToListAsync();

        return Ok(providers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OidcProvider>> Get(int id)
    {
        var provider = await _context.OidcProviders.FindAsync(id);
        if (provider == null) return NotFound();

        provider.ClientSecret = "********";
        return Ok(provider);
    }

    [HttpPost]
    public async Task<ActionResult<OidcProvider>> Create([FromBody] CreateOidcProviderRequest request)
    {
        if (await _context.OidcProviders.AnyAsync(p => p.Name == request.Name))
        {
            return BadRequest(new { error = "Provider with this name already exists" });
        }

        var provider = new OidcProvider
        {
            Name = request.Name.ToLowerInvariant().Replace(" ", "-"),
            DisplayName = request.DisplayName,
            Authority = request.Authority,
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
            Enabled = request.Enabled,
            AutoCreateUsers = request.AutoCreateUsers,
            DefaultRole = request.DefaultRole ?? DbSeeder.Roles.Reader,
            UsernameClaim = request.UsernameClaim ?? "preferred_username",
            EmailClaim = request.EmailClaim ?? "email",
            DisplayNameClaim = request.DisplayNameClaim ?? "name",
            RolesClaim = request.RolesClaim,
            IconUrl = request.IconUrl,
            ButtonColor = request.ButtonColor
        };

        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = provider.Id }, provider);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOidcProviderRequest request)
    {
        var provider = await _context.OidcProviders.FindAsync(id);
        if (provider == null) return NotFound();

        provider.DisplayName = request.DisplayName ?? provider.DisplayName;
        provider.Authority = request.Authority ?? provider.Authority;
        provider.ClientId = request.ClientId ?? provider.ClientId;
        provider.Enabled = request.Enabled ?? provider.Enabled;
        provider.AutoCreateUsers = request.AutoCreateUsers ?? provider.AutoCreateUsers;
        provider.DefaultRole = request.DefaultRole ?? provider.DefaultRole;
        provider.IconUrl = request.IconUrl;
        provider.ButtonColor = request.ButtonColor;
        provider.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.ClientSecret))
        {
            provider.ClientSecret = request.ClientSecret;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var provider = await _context.OidcProviders.FindAsync(id);
        if (provider == null) return NotFound();

        _context.OidcProviders.Remove(provider);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class OidcProviderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Authority { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public bool Enabled { get; set; }
    public bool AutoCreateUsers { get; set; }
    public string DefaultRole { get; set; } = null!;
    public string? IconUrl { get; set; }
    public string? ButtonColor { get; set; }
}

public class CreateOidcProviderRequest
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public bool Enabled { get; set; } = true;
    public bool AutoCreateUsers { get; set; } = true;
    public string? DefaultRole { get; set; }
    public string? UsernameClaim { get; set; }
    public string? EmailClaim { get; set; }
    public string? DisplayNameClaim { get; set; }
    public string? RolesClaim { get; set; }
    public string? IconUrl { get; set; }
    public string? ButtonColor { get; set; }
}

public class UpdateOidcProviderRequest
{
    public string? DisplayName { get; set; }
    public string? Authority { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public bool? Enabled { get; set; }
    public bool? AutoCreateUsers { get; set; }
    public string? DefaultRole { get; set; }
    public string? IconUrl { get; set; }
    public string? ButtonColor { get; set; }
}
```

### Step 6.6: Update Program.cs for OIDC

Add the following to `Program.cs`:

```csharp
// Add these services after existing services
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IOidcService, OidcService>();
```

### Step 6.7: Create EF Core Migration

```bash
cd src/Api
dotnet ef migrations add AddOidcSupport
dotnet ef database update
```

---

## Phase 7: React Frontend with TanStack Router & Axios

### Step 7.1: Install Dependencies

```bash
cd src/frontend
npm install @tanstack/react-router axios zustand
```

### Step 7.2: Create Axios Instance with Interceptors

Create `src/lib/api.ts`:

```typescript
import axios from 'axios';
import { useAuth } from '../hooks/useAuth';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - adds auth token to all requests
api.interceptors.request.use(
  (config) => {
    const { accessToken } = useAuth.getState();
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handles 401 errors globally
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      const { logout } = useAuth.getState();
      logout();
      
      // Redirect to login if not already there
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
```

### Step 7.3: Create Auth Hook

Create `src/hooks/useAuth.ts`:

```typescript
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import api from '../lib/api';

interface User {
  id: string;
  username: string;
  displayName?: string;
  roles: string[];
}

interface OidcProvider {
  name: string;
  displayName: string;
  iconUrl?: string;
  buttonColor?: string;
  loginUrl: string;
}

interface AuthState {
  accessToken: string | null;
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  oidcProviders: OidcProvider[];
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  hasRole: (role: string) => boolean;
  fetchOidcProviders: () => Promise<void>;
  setTokenFromCallback: (token: string) => Promise<void>;
  checkAuth: () => Promise<boolean>;
}

export const useAuth = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      user: null,
      isAuthenticated: false,
      isLoading: true,
      oidcProviders: [],

      login: async (username: string, password: string) => {
        const response = await api.post('/api/auth/login', { username, password });
        const data = response.data;
        
        set({
          accessToken: data.accessToken,
          user: data.user,
          isAuthenticated: true,
          isLoading: false,
        });
      },

      logout: () => {
        set({
          accessToken: null,
          user: null,
          isAuthenticated: false,
          isLoading: false,
        });
      },

      hasRole: (role: string) => {
        const { user } = get();
        return user?.roles.includes(role) ?? false;
      },

      fetchOidcProviders: async () => {
        try {
          const response = await api.get('/api/auth/oidc/providers');
          set({ oidcProviders: response.data });
        } catch (error) {
          console.error('Failed to fetch OIDC providers:', error);
        }
      },

      setTokenFromCallback: async (token: string) => {
        // Temporarily set token to make the request
        set({ accessToken: token });
        
        try {
          const response = await api.get('/api/auth/me');
          set({
            user: response.data,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch (error) {
          set({ accessToken: null, isLoading: false });
          throw new Error('Failed to fetch user info');
        }
      },

      checkAuth: async () => {
        const { accessToken } = get();
        
        if (!accessToken) {
          set({ isLoading: false, isAuthenticated: false });
          return false;
        }

        try {
          const response = await api.get('/api/auth/me');
          set({
            user: response.data,
            isAuthenticated: true,
            isLoading: false,
          });
          return true;
        } catch {
          set({
            accessToken: null,
            user: null,
            isAuthenticated: false,
            isLoading: false,
          });
          return false;
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        accessToken: state.accessToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
      onRehydrateStorage: () => (state) => {
        // After rehydration, verify the token is still valid
        state?.checkAuth();
      },
    }
  )
);
```

### Step 7.4: Create TanStack Router Configuration

Create `src/routes/__root.tsx`:

```tsx
import { createRootRoute, Outlet } from '@tanstack/react-router';

export const Route = createRootRoute({
  component: () => <Outlet />,
});
```

Create `src/routes/_authenticated.tsx`:

```tsx
import { createFileRoute, Outlet, redirect } from '@tanstack/react-router';
import { useAuth } from '../hooks/useAuth';

export const Route = createFileRoute('/_authenticated')({
  beforeLoad: async ({ location }) => {
    const { isAuthenticated, checkAuth } = useAuth.getState();
    
    // If not authenticated, try to verify existing token
    if (!isAuthenticated) {
      const isValid = await checkAuth();
      if (!isValid) {
        throw redirect({
          to: '/login',
          search: {
            redirect: location.href,
          },
        });
      }
    }
  },
  component: AuthenticatedLayout,
});

function AuthenticatedLayout() {
  return (
    <div className="authenticated-layout">
      <Outlet />
    </div>
  );
}
```

Create `src/routes/_authenticated/index.tsx` (Dashboard/Home):

```tsx
import { createFileRoute } from '@tanstack/react-router';
import { useAuth } from '../../hooks/useAuth';

export const Route = createFileRoute('/_authenticated/')({
  component: Dashboard,
});

function Dashboard() {
  const { user, logout, hasRole } = useAuth();

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1>Dashboard</h1>
        <div className="user-info">
          <span>Welcome, {user?.displayName || user?.username}!</span>
          <button onClick={logout} className="logout-button">
            Logout
          </button>
        </div>
      </header>

      <main className="dashboard-content">
        <div className="user-card">
          <h2>Your Profile</h2>
          <p><strong>Username:</strong> {user?.username}</p>
          <p><strong>Display Name:</strong> {user?.displayName || 'Not set'}</p>
          <p><strong>Roles:</strong> {user?.roles.join(', ')}</p>
        </div>

        {hasRole('Admin') && (
          <div className="admin-section">
            <h2>Admin Section</h2>
            <p>This content is only visible to administrators.</p>
          </div>
        )}
      </main>
    </div>
  );
}
```

Create `src/routes/login.tsx`:

```tsx
import { createFileRoute, useNavigate, useSearch } from '@tanstack/react-router';
import { useEffect, useState } from 'react';
import { useAuth } from '../hooks/useAuth';

type LoginSearch = {
  redirect?: string;
  error?: string;
};

export const Route = createFileRoute('/login')({
  validateSearch: (search: Record<string, unknown>): LoginSearch => ({
    redirect: search.redirect as string | undefined,
    error: search.error as string | undefined,
  }),
  component: LoginPage,
});

function LoginPage() {
  const navigate = useNavigate();
  const search = useSearch({ from: '/login' });
  const { login, isAuthenticated, oidcProviders, fetchOidcProviders } = useAuth();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState(search.error || '');
  const [loading, setLoading] = useState(false);

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate({ to: search.redirect || '/' });
    }
  }, [isAuthenticated, navigate, search.redirect]);

  // Fetch OIDC providers on mount
  useEffect(() => {
    fetchOidcProviders();
  }, [fetchOidcProviders]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      await login(username, password);
      navigate({ to: search.redirect || '/' });
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Login failed. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="login-card">
          <h1>Sign In</h1>
          <p className="login-subtitle">Enter your credentials to continue</p>

          {error && (
            <div className="error-message">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="login-form">
            <div className="form-group">
              <label htmlFor="username">Username</label>
              <input
                id="username"
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Enter your username"
                required
                autoComplete="username"
                autoFocus
              />
            </div>

            <div className="form-group">
              <label htmlFor="password">Password</label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                required
                autoComplete="current-password"
              />
            </div>

            <button 
              type="submit" 
              disabled={loading} 
              className="login-button"
            >
              {loading ? 'Signing in...' : 'Sign In'}
            </button>
          </form>

          {oidcProviders.length > 0 && (
            <>
              <div className="divider">
                <span>or continue with</span>
              </div>

              <div className="oidc-providers">
                {oidcProviders.map((provider) => (
                  <a
                    key={provider.name}
                    href={`${provider.loginUrl}?returnUrl=${encodeURIComponent(search.redirect || '/')}`}
                    className="oidc-button"
                    style={{ 
                      backgroundColor: provider.buttonColor || '#4a5568' 
                    }}
                  >
                    {provider.iconUrl && (
                      <img 
                        src={provider.iconUrl} 
                        alt="" 
                        className="provider-icon" 
                      />
                    )}
                    {provider.displayName}
                  </a>
                ))}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
```

Create `src/routes/auth.callback.tsx` (OIDC Callback):

```tsx
import { createFileRoute, useNavigate, useSearch } from '@tanstack/react-router';
import { useEffect, useState } from 'react';
import { useAuth } from '../hooks/useAuth';

type CallbackSearch = {
  token?: string;
  error?: string;
};

export const Route = createFileRoute('/auth/callback')({
  validateSearch: (search: Record<string, unknown>): CallbackSearch => ({
    token: search.token as string | undefined,
    error: search.error as string | undefined,
  }),
  component: AuthCallback,
});

function AuthCallback() {
  const navigate = useNavigate();
  const search = useSearch({ from: '/auth/callback' });
  const { setTokenFromCallback } = useAuth();
  const [error, setError] = useState<string | null>(search.error || null);

  useEffect(() => {
    if (search.error) {
      setError(search.error);
      return;
    }

    if (search.token) {
      setTokenFromCallback(search.token)
        .then(() => navigate({ to: '/' }))
        .catch((err) => setError(err.message));
    } else {
      setError('No authentication token received');
    }
  }, [search.token, search.error, setTokenFromCallback, navigate]);

  if (error) {
    return (
      <div className="callback-page">
        <div className="callback-card error">
          <h2>Authentication Failed</h2>
          <p>{error}</p>
          <a href="/login" className="back-to-login">
            Return to Login
          </a>
        </div>
      </div>
    );
  }

  return (
    <div className="callback-page">
      <div className="callback-card">
        <div className="spinner" />
        <p>Completing sign in...</p>
      </div>
    </div>
  );
}
```

### Step 7.5: Create Router Instance

Create `src/router.tsx`:

```tsx
import { createRouter } from '@tanstack/react-router';
import { routeTree } from './routeTree.gen';

export const router = createRouter({ 
  routeTree,
  defaultPreload: 'intent',
});

// Register router for type safety
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
```

### Step 7.6: Update Main Entry Point

Update `src/main.tsx`:

```tsx
import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { router } from './router';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>
);
```

### Step 7.7: Add Basic Styles

Update `src/index.css`:

```css
* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen,
    Ubuntu, Cantarell, sans-serif;
  background-color: #f5f5f5;
  color: #333;
  line-height: 1.6;
}

/* Login Page */
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.login-container {
  width: 100%;
  max-width: 400px;
}

.login-card {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

.login-card h1 {
  margin-bottom: 0.5rem;
  font-size: 1.5rem;
}

.login-subtitle {
  color: #666;
  margin-bottom: 1.5rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-weight: 500;
  font-size: 0.875rem;
}

.form-group input {
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
  transition: border-color 0.2s;
}

.form-group input:focus {
  outline: none;
  border-color: #007bff;
}

.login-button {
  padding: 0.75rem;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.login-button:hover:not(:disabled) {
  background: #0056b3;
}

.login-button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.error-message {
  background: #fee;
  color: #c00;
  padding: 0.75rem;
  border-radius: 4px;
  margin-bottom: 1rem;
  font-size: 0.875rem;
}

/* Divider */
.divider {
  display: flex;
  align-items: center;
  margin: 1.5rem 0;
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: #ddd;
}

.divider span {
  padding: 0 1rem;
  color: #666;
  font-size: 0.875rem;
}

/* OIDC Providers */
.oidc-providers {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.oidc-button {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem;
  color: white;
  text-decoration: none;
  border-radius: 4px;
  font-weight: 500;
  transition: opacity 0.2s;
}

.oidc-button:hover {
  opacity: 0.9;
}

.provider-icon {
  width: 20px;
  height: 20px;
}

/* Dashboard */
.dashboard {
  min-height: 100vh;
}

.dashboard-header {
  background: white;
  padding: 1rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.dashboard-header h1 {
  font-size: 1.25rem;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.logout-button {
  padding: 0.5rem 1rem;
  background: #dc3545;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.875rem;
}

.logout-button:hover {
  background: #c82333;
}

.dashboard-content {
  padding: 2rem;
  max-width: 800px;
  margin: 0 auto;
}

.user-card,
.admin-section {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
}

.user-card h2,
.admin-section h2 {
  margin-bottom: 1rem;
  font-size: 1.125rem;
}

.user-card p {
  margin-bottom: 0.5rem;
}

.admin-section {
  border-left: 4px solid #007bff;
}

/* Callback Page */
.callback-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.callback-card {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  text-align: center;
}

.callback-card.error {
  border-top: 4px solid #dc3545;
}

.callback-card h2 {
  margin-bottom: 1rem;
  color: #dc3545;
}

.back-to-login {
  display: inline-block;
  margin-top: 1rem;
  color: #007bff;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #f3f3f3;
  border-top: 3px solid #007bff;
  border-radius: 50%;
  margin: 0 auto 1rem;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}
```

### Step 7.8: Generate Route Tree

Add the TanStack Router plugin to your Vite config.

Update `vite.config.ts`:

```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { TanStackRouterVite } from '@tanstack/router-plugin/vite';

export default defineConfig({
  plugins: [
    TanStackRouterVite(),
    react(),
  ],
});
```

Install the router plugin:

```bash
npm install -D @tanstack/router-plugin
```

The route tree will be auto-generated when you run the dev server.

### Step 7.9: Project Structure Summary

Your frontend `src` folder should look like this:

```
src/
├── hooks/
│   └── useAuth.ts
├── lib/
│   └── api.ts
├── routes/
│   ├── __root.tsx
│   ├── _authenticated.tsx
│   ├── _authenticated/
│   │   └── index.tsx
│   ├── auth.callback.tsx
│   └── login.tsx
├── router.tsx
├── main.tsx
├── index.css
└── routeTree.gen.ts  (auto-generated)
```

### Step 7.10: Adding More Protected Routes

To add more protected pages, create new files under `src/routes/_authenticated/`. For example:

Create `src/routes/_authenticated/settings.tsx`:

```tsx
import { createFileRoute } from '@tanstack/react-router';
import { useAuth } from '../../hooks/useAuth';

export const Route = createFileRoute('/_authenticated/settings')({
  component: SettingsPage,
});

function SettingsPage() {
  const { user } = useAuth();

  return (
    <div className="settings-page">
      <h1>Settings</h1>
      <p>Manage your account settings here, {user?.username}.</p>
    </div>
  );
}
```

Create `src/routes/_authenticated/admin.tsx` (Admin-only route):

```tsx
import { createFileRoute, redirect } from '@tanstack/react-router';
import { useAuth } from '../../hooks/useAuth';

export const Route = createFileRoute('/_authenticated/admin')({
  beforeLoad: () => {
    const { hasRole } = useAuth.getState();
    if (!hasRole('Admin')) {
      throw redirect({ to: '/' });
    }
  },
  component: AdminPage,
});

function AdminPage() {
  return (
    <div className="admin-page">
      <h1>Admin Panel</h1>
      <p>This page is only accessible to administrators.</p>
    </div>
  );
}
```

### Step 7.11: How Route Protection Works

The authentication flow works as follows:

1. **Root Layout** (`__root.tsx`) - Renders all routes
2. **Authenticated Layout** (`_authenticated.tsx`) - Wraps all protected routes
   - `beforeLoad` checks if user is authenticated
   - If not, redirects to `/login` with the original URL as a `redirect` param
3. **Login Page** (`login.tsx`) - Public route
   - After successful login, redirects back to the original URL or `/`
4. **Protected Routes** (`_authenticated/*.tsx`) - All routes under this folder are protected

```
URL: /settings
    │
    ▼
_authenticated.tsx (beforeLoad check)
    │
    ├── Not authenticated? → Redirect to /login?redirect=/settings
    │
    └── Authenticated? → Render _authenticated/settings.tsx
```

### Step 7.12: Using the API Client in Components

Use the configured Axios instance for all API calls:

```tsx
import api from '../lib/api';

// In a component or hook
const fetchData = async () => {
  try {
    const response = await api.get('/api/data');
    return response.data;
  } catch (error) {
    // 401 errors are automatically handled by the interceptor
    console.error('Failed to fetch data:', error);
  }
};

// POST example
const createItem = async (data: CreateItemRequest) => {
  const response = await api.post('/api/items', data);
  return response.data;
};
```

---

## Phase 8: Refresh Tokens & Server-side Logout

This phase adds proper token refresh functionality and server-side logout for both local accounts and OIDC users.

### Why Refresh Tokens Matter for OIDC Too

When a user logs in via OIDC (Authentik), your API issues its own JWT. That JWT expires (default: 1 hour). Without refresh tokens, the user would need to go through the entire OIDC flow again. With refresh tokens, you silently get a new JWT.

```
OIDC Login → Your API JWT (1 hour) → Expires → Refresh Token → New JWT ✓
                                              └─ Without refresh → Full OIDC flow again ✗
```

### Step 8.1: Create RefreshToken Entity

Create `Models/RefreshToken.cs`:

```csharp
namespace Api.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    
    // Track which token replaced this one (for rotation)
    public string? ReplacedByToken { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
```

### Step 8.2: Update Database Context

Update `Data/ApplicationDbContext.cs` to add the RefreshToken DbSet:

```csharp
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

Add to `OnModelCreating`:

```csharp
builder.Entity<RefreshToken>(entity =>
{
    entity.HasIndex(e => e.Token).IsUnique();
    entity.HasOne(e => e.User)
          .WithMany()
          .HasForeignKey(e => e.UserId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

### Step 8.3: Create Migration

```bash
cd src/Api
dotnet ef migrations add AddRefreshTokens
dotnet ef database update
```

### Step 8.4: Create Token Models

Add to `Models/Auth/TokenModels.cs`:

```csharp
namespace Api.Models.Auth;

public record RefreshTokenRequest(string RefreshToken);

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt
);
```

### Step 8.5: Update Token Service

Update `Services/ITokenService.cs`:

```csharp
using Api.Models;

namespace Api.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    Task<RefreshToken> GenerateRefreshTokenAsync(ApplicationUser user);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(RefreshToken token, string reason, string? replacedByToken = null);
    Task RevokeAllUserTokensAsync(string userId, string reason);
    DateTime GetAccessTokenExpiry();
}
```

Update `Services/TokenService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public TokenService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = GetAccessTokenExpiry();

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(ApplicationUser user)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var refreshTokenDays = _configuration.GetValue("Jwt:RefreshTokenExpiryDays", 7);

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token);
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken token, string reason, string? replacedByToken = null)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedReason = reason;
        token.ReplacedByToken = replacedByToken;
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(string userId, string reason)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
        }

        await _context.SaveChangesAsync();
    }

    public DateTime GetAccessTokenExpiry()
    {
        var minutes = _configuration.GetValue("Jwt:AccessTokenExpiryMinutes", 60);
        return DateTime.UtcNow.AddMinutes(minutes);
    }
}
```

### Step 8.6: Update Auth Controller

Add these endpoints to `Controllers/AuthController.cs`:

```csharp
[HttpPost("refresh")]
public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
{
    var refreshToken = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);

    if (refreshToken == null)
    {
        return Unauthorized(new { message = "Invalid refresh token" });
    }

    if (!refreshToken.IsActive)
    {
        return Unauthorized(new { message = "Refresh token expired or revoked" });
    }

    var user = refreshToken.User;

    // Rotate refresh token (revoke old, create new)
    var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user);
    await _tokenService.RevokeRefreshTokenAsync(
        refreshToken, 
        "Replaced by new token", 
        newRefreshToken.Token);

    // Generate new access token
    var roles = await _userManager.GetRolesAsync(user);
    var accessToken = _tokenService.GenerateAccessToken(user, roles);

    _logger.LogInformation("Token refreshed for user {Username}", user.UserName);

    return Ok(new LoginResponse(
        AccessToken: accessToken,
        RefreshToken: newRefreshToken.Token,
        ExpiresAt: _tokenService.GetAccessTokenExpiry(),
        User: new UserInfo(user.Id, user.UserName!, user.DisplayName, roles)
    ));
}

[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (userId == null)
    {
        return Unauthorized();
    }

    if (!string.IsNullOrEmpty(request?.RefreshToken))
    {
        // Revoke specific refresh token
        var refreshToken = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);
        if (refreshToken != null && refreshToken.UserId == userId)
        {
            await _tokenService.RevokeRefreshTokenAsync(refreshToken, "User logout");
        }
    }
    else
    {
        // Revoke all user's refresh tokens
        await _tokenService.RevokeAllUserTokensAsync(userId, "User logout (all devices)");
    }

    _logger.LogInformation("User {UserId} logged out", userId);

    return Ok(new { message = "Logged out successfully" });
}
```

Add the LogoutRequest record:

```csharp
public record LogoutRequest(string? RefreshToken);
```

Update the existing `Login` endpoint to use the new refresh token method:

```csharp
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    var user = await _userManager.FindByNameAsync(request.Username);
    
    if (user is null)
    {
        return Unauthorized(new { message = "Invalid username or password" });
    }

    var result = await _signInManager.CheckPasswordSignInAsync(
        user, request.Password, lockoutOnFailure: true);
    
    if (!result.Succeeded)
    {
        if (result.IsLockedOut)
        {
            return Unauthorized(new { message = "Account is locked. Try again later." });
        }
        return Unauthorized(new { message = "Invalid username or password" });
    }

    // Update last login
    user.LastLoginAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    var roles = await _userManager.GetRolesAsync(user);
    var accessToken = _tokenService.GenerateAccessToken(user, roles);
    var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

    _logger.LogInformation("User {Username} logged in successfully", user.UserName);

    return Ok(new LoginResponse(
        AccessToken: accessToken,
        RefreshToken: refreshToken.Token,
        ExpiresAt: _tokenService.GetAccessTokenExpiry(),
        User: new UserInfo(user.Id, user.UserName!, user.DisplayName, roles)
    ));
}
```

### Step 8.7: Update OIDC Service

Update the `HandleCallbackAsync` method in `Services/OidcService.cs` to also generate refresh tokens:

```csharp
public async Task<LoginResponse> HandleCallbackAsync(string providerName, string code, string state)
{
    // ... existing code until user is retrieved ...

    var roles = await _userManager.GetRolesAsync(user);
    var accessToken = _tokenService.GenerateAccessToken(user, roles);
    var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

    _logger.LogInformation(
        "User {Username} logged in via OIDC provider {Provider}",
        user.UserName, providerName);

    return new LoginResponse(
        AccessToken: accessToken,
        RefreshToken: refreshToken.Token,
        ExpiresAt: _tokenService.GetAccessTokenExpiry(),
        User: new UserInfo(user.Id, user.UserName!, user.DisplayName, roles)
    );
}
```

### Step 8.8: Update appsettings.json

```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-that-should-be-at-least-32-characters-long",
    "Issuer": "myapp-api",
    "Audience": "myapp-client",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

### Step 8.9: Update React API Client with Auto-Refresh

Update `src/lib/api.ts`:

```typescript
import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { useAuth } from '../hooks/useAuth';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Flag to prevent multiple refresh attempts
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: Error) => void;
}> = [];

const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve(token!);
    }
  });
  failedQueue = [];
};

// Request interceptor - adds auth token
api.interceptors.request.use(
  (config) => {
    const { accessToken } = useAuth.getState();
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handles 401 and auto-refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // If error is not 401 or request already retried, reject
    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Don't try to refresh if we're already on the refresh endpoint
    if (originalRequest.url?.includes('/auth/refresh')) {
      useAuth.getState().logout();
      return Promise.reject(error);
    }

    if (isRefreshing) {
      // Wait for the refresh to complete
      return new Promise((resolve, reject) => {
        failedQueue.push({
          resolve: (token: string) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            resolve(api(originalRequest));
          },
          reject: (err: Error) => {
            reject(err);
          },
        });
      });
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const { refreshToken, setTokens, logout } = useAuth.getState();

      if (!refreshToken) {
        throw new Error('No refresh token available');
      }

      const response = await axios.post(`${API_URL}/api/auth/refresh`, {
        refreshToken,
      });

      const { accessToken: newAccessToken, refreshToken: newRefreshToken, user } = response.data;

      setTokens(newAccessToken, newRefreshToken, user);
      processQueue(null, newAccessToken);

      originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
      return api(originalRequest);
    } catch (refreshError) {
      processQueue(refreshError as Error, null);
      useAuth.getState().logout();
      
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
      
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

export default api;
```

### Step 8.10: Update Auth Hook for Refresh Tokens

Update `src/hooks/useAuth.ts`:

```typescript
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import api from '../lib/api';

interface User {
  id: string;
  username: string;
  displayName?: string;
  roles: string[];
}

interface OidcProvider {
  name: string;
  displayName: string;
  iconUrl?: string;
  buttonColor?: string;
  loginUrl: string;
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  oidcProviders: OidcProvider[];
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
  fetchOidcProviders: () => Promise<void>;
  setTokenFromCallback: (token: string, refreshToken: string) => Promise<void>;
  setTokens: (accessToken: string, refreshToken: string, user: User) => void;
  checkAuth: () => Promise<boolean>;
}

export const useAuth = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      isLoading: true,
      oidcProviders: [],

      login: async (username: string, password: string) => {
        const response = await api.post('/api/auth/login', { username, password });
        const data = response.data;
        
        set({
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
          user: data.user,
          isAuthenticated: true,
          isLoading: false,
        });
      },

      logout: async () => {
        const { refreshToken } = get();
        
        try {
          // Server-side logout
          await api.post('/api/auth/logout', { refreshToken });
        } catch (error) {
          // Continue with client-side logout even if server logout fails
          console.error('Server logout failed:', error);
        }

        set({
          accessToken: null,
          refreshToken: null,
          user: null,
          isAuthenticated: false,
          isLoading: false,
        });
      },

      hasRole: (role: string) => {
        const { user } = get();
        return user?.roles.includes(role) ?? false;
      },

      fetchOidcProviders: async () => {
        try {
          const response = await api.get('/api/auth/oidc/providers');
          set({ oidcProviders: response.data });
        } catch (error) {
          console.error('Failed to fetch OIDC providers:', error);
        }
      },

      setTokenFromCallback: async (accessToken: string, refreshToken: string) => {
        set({ accessToken, refreshToken });
        
        try {
          const response = await api.get('/api/auth/me');
          set({
            user: response.data,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch (error) {
          set({ accessToken: null, refreshToken: null, isLoading: false });
          throw new Error('Failed to fetch user info');
        }
      },

      setTokens: (accessToken: string, refreshToken: string, user: User) => {
        set({
          accessToken,
          refreshToken,
          user,
          isAuthenticated: true,
        });
      },

      checkAuth: async () => {
        const { accessToken } = get();
        
        if (!accessToken) {
          set({ isLoading: false, isAuthenticated: false });
          return false;
        }

        try {
          const response = await api.get('/api/auth/me');
          set({
            user: response.data,
            isAuthenticated: true,
            isLoading: false,
          });
          return true;
        } catch {
          // Token refresh will be attempted automatically by the interceptor
          // If we still fail here, the interceptor will handle logout
          set({ isLoading: false });
          return false;
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
      onRehydrateStorage: () => (state) => {
        state?.checkAuth();
      },
    }
  )
);
```

### Step 8.11: Update OIDC Callback to Handle Refresh Token

Update `src/routes/auth.callback.tsx`:

```tsx
import { createFileRoute, useNavigate, useSearch } from '@tanstack/react-router';
import { useEffect, useState } from 'react';
import { useAuth } from '../hooks/useAuth';

type CallbackSearch = {
  token?: string;
  refreshToken?: string;
  error?: string;
};

export const Route = createFileRoute('/auth/callback')({
  validateSearch: (search: Record<string, unknown>): CallbackSearch => ({
    token: search.token as string | undefined,
    refreshToken: search.refreshToken as string | undefined,
    error: search.error as string | undefined,
  }),
  component: AuthCallback,
});

function AuthCallback() {
  const navigate = useNavigate();
  const search = useSearch({ from: '/auth/callback' });
  const { setTokenFromCallback } = useAuth();
  const [error, setError] = useState<string | null>(search.error || null);

  useEffect(() => {
    if (search.error) {
      setError(search.error);
      return;
    }

    if (search.token && search.refreshToken) {
      setTokenFromCallback(search.token, search.refreshToken)
        .then(() => navigate({ to: '/' }))
        .catch((err) => setError(err.message));
    } else {
      setError('No authentication token received');
    }
  }, [search.token, search.refreshToken, search.error, setTokenFromCallback, navigate]);

  // ... rest of component stays the same
}
```

Update the OIDC callback endpoint in `Controllers/OidcController.cs` to pass refresh token:

```csharp
return Redirect(
    $"{frontendUrl}/auth/callback?token={loginResponse.AccessToken}&refreshToken={Uri.EscapeDataString(loginResponse.RefreshToken)}");
```

### Step 8.12: Add Password Reset (Admin-triggered)

For local accounts, an admin can reset a user's password. The new temporary password is logged for the admin to share with the user. OIDC-only users should use their identity provider's reset flow.

Add to `Controllers/AuthController.cs`:

```csharp
[HttpPost("reset-password/{userId}")]
[Authorize(Roles = DbSeeder.Roles.Admin)]
public async Task<IActionResult> ResetPassword(string userId)
{
    var user = await _userManager.FindByIdAsync(userId);
    
    if (user is null)
    {
        return NotFound(new { message = "User not found" });
    }

    // Check if user has a password (local account)
    var hasPassword = await _userManager.HasPasswordAsync(user);
    
    if (!hasPassword)
    {
        return BadRequest(new { 
            message = "This user uses external authentication (OIDC). " +
                      "Password must be reset through their identity provider." 
        });
    }

    // Generate a random temporary password
    var tempPassword = GenerateTemporaryPassword();

    // Remove existing password and set new one
    var removeResult = await _userManager.RemovePasswordAsync(user);
    if (!removeResult.Succeeded)
    {
        return BadRequest(new { errors = removeResult.Errors.Select(e => e.Description) });
    }

    var addResult = await _userManager.AddPasswordAsync(user, tempPassword);
    if (!addResult.Succeeded)
    {
        return BadRequest(new { errors = addResult.Errors.Select(e => e.Description) });
    }

    // Revoke all refresh tokens so user must re-login
    await _tokenService.RevokeAllUserTokensAsync(userId, "Password reset by admin");

    // Log the temporary password for the admin
    _logger.LogWarning(
        "Password reset for user {Username} (ID: {UserId}). Temporary password: {TempPassword}",
        user.UserName, userId, tempPassword);

    return Ok(new { 
        message = $"Password reset successful for {user.UserName}. Check the server logs for the temporary password.",
        username = user.UserName
    });
}

private static string GenerateTemporaryPassword()
{
    // Generate a readable temporary password: 3 words + 2 digits
    var words = new[] { 
        "Apple", "Banana", "Cherry", "Dragon", "Eagle", "Forest", 
        "Garden", "Harbor", "Island", "Jungle", "Kitten", "Lemon",
        "Mountain", "Neptune", "Ocean", "Panda", "Queen", "River",
        "Silver", "Thunder", "Umbrella", "Valley", "Winter", "Yellow"
    };
    
    var random = Random.Shared;
    var word1 = words[random.Next(words.Length)];
    var word2 = words[random.Next(words.Length)];
    var digits = random.Next(10, 99);
    
    return $"{word1}{word2}{digits}";
}
```

### Step 8.13: Add User Management Endpoints (Admin)

Add a controller for basic user management. Create `Controllers/Admin/UsersController.cs`:

```csharp
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = DbSeeder.Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ITokenService tokenService,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var hasPassword = await _userManager.HasPasswordAsync(user);
            var externalLogins = await _context.ExternalLogins
                .Where(e => e.UserId == user.Id)
                .Select(e => e.Provider)
                .ToListAsync();

            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Roles = roles,
                HasLocalAccount = hasPassword,
                ExternalProviders = externalLogins,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        return Ok(userDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> Get(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var hasPassword = await _userManager.HasPasswordAsync(user);
        var externalLogins = await _context.ExternalLogins
            .Where(e => e.UserId == user.Id)
            .Select(e => e.Provider)
            .ToListAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = roles,
            HasLocalAccount = hasPassword,
            ExternalProviders = externalLogins,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(string id, [FromBody] UpdateRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        
        // Remove roles not in new list
        var rolesToRemove = currentRoles.Except(request.Roles);
        await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

        // Add new roles
        var rolesToAdd = request.Roles.Except(currentRoles);
        await _userManager.AddToRolesAsync(user, rolesToAdd);

        _logger.LogInformation(
            "Roles updated for user {Username}: {Roles}", 
            user.UserName, string.Join(", ", request.Roles));

        return Ok(new { message = "Roles updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Prevent deleting yourself
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (id == currentUserId)
        {
            return BadRequest(new { message = "You cannot delete your own account" });
        }

        // Revoke all tokens first
        await _tokenService.RevokeAllUserTokensAsync(id, "User deleted");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {Username} (ID: {UserId}) deleted", user.UserName, id);

        return Ok(new { message = "User deleted successfully" });
    }
}

public class UserDto
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];
    public bool HasLocalAccount { get; set; }
    public IEnumerable<string> ExternalProviders { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public record UpdateRolesRequest(IEnumerable<string> Roles);
```

### Password Reset Flow Summary

| User Type | Reset Method |
|-----------|--------------|
| **Local account** | Admin calls `/api/auth/reset-password/{userId}` → Temp password logged → User logs in and changes password |
| **OIDC only** | User uses identity provider's reset flow (e.g., Authentik's "Forgot Password") |
| **Both (linked)** | Either method works - admin can reset local password, or user resets via OIDC provider |

### Example: Reset a User's Password

```bash
# As admin, get user list
curl http://localhost:5000/api/admin/users \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Reset password for a specific user
curl -X POST http://localhost:5000/api/auth/reset-password/user-id-here \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Check server logs for the temporary password:
# warn: Api.Controllers.AuthController
#       Password reset for user john (ID: abc123). Temporary password: AppleForest42
```

### Step 8.14: Add Account Profile Update

Allow users to update their own display name and email.

Add to `Models/Auth/ProfileModels.cs`:

```csharp
namespace Api.Models.Auth;

public record UpdateProfileRequest(
    string? DisplayName,
    string? Email
);

public record ProfileResponse(
    string Id,
    string Username,
    string? Email,
    string? DisplayName,
    bool HasLocalAccount,
    IEnumerable<string> ExternalProviders,
    IEnumerable<string> Roles
);
```

Add these endpoints to `Controllers/AuthController.cs`:

```csharp
[HttpGet("profile")]
[Authorize]
public async Task<ActionResult<ProfileResponse>> GetProfile()
{
    var user = await _userManager.GetUserAsync(User);
    
    if (user is null)
    {
        return Unauthorized();
    }

    var roles = await _userManager.GetRolesAsync(user);
    var hasPassword = await _userManager.HasPasswordAsync(user);
    var externalLogins = await _context.ExternalLogins
        .Where(e => e.UserId == user.Id)
        .Select(e => e.Provider)
        .ToListAsync();

    return Ok(new ProfileResponse(
        Id: user.Id,
        Username: user.UserName!,
        Email: user.Email,
        DisplayName: user.DisplayName,
        HasLocalAccount: hasPassword,
        ExternalProviders: externalLogins,
        Roles: roles
    ));
}

[HttpPut("profile")]
[Authorize]
public async Task<ActionResult<ProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
{
    var user = await _userManager.GetUserAsync(User);
    
    if (user is null)
    {
        return Unauthorized();
    }

    var updated = false;

    // Update display name
    if (request.DisplayName != null && request.DisplayName != user.DisplayName)
    {
        user.DisplayName = request.DisplayName;
        updated = true;
    }

    // Update email
    if (request.Email != null && request.Email != user.Email)
    {
        // Check if email is already in use
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null && existingUser.Id != user.Id)
        {
            return BadRequest(new { message = "Email is already in use" });
        }

        user.Email = request.Email;
        user.EmailConfirmed = false; // Reset confirmation if you add email verification later
        updated = true;
    }

    if (updated)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {Username} updated their profile", user.UserName);
    }

    // Return updated profile
    var roles = await _userManager.GetRolesAsync(user);
    var hasPassword = await _userManager.HasPasswordAsync(user);
    var externalLogins = await _context.ExternalLogins
        .Where(e => e.UserId == user.Id)
        .Select(e => e.Provider)
        .ToListAsync();

    return Ok(new ProfileResponse(
        Id: user.Id,
        Username: user.UserName!,
        Email: user.Email,
        DisplayName: user.DisplayName,
        HasLocalAccount: hasPassword,
        ExternalProviders: externalLogins,
        Roles: roles
    ));
}
```

Add the `ApplicationDbContext` dependency to the `AuthController` constructor:

```csharp
private readonly ApplicationDbContext _context;

public AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    ApplicationDbContext context,  // Add this
    ILogger<AuthController> logger)
{
    _userManager = userManager;
    _signInManager = signInManager;
    _tokenService = tokenService;
    _context = context;  // Add this
    _logger = logger;
}
```

### Step 8.15: Add Profile Update to React

Add to `src/hooks/useAuth.ts`:

```typescript
interface AuthState {
  // ... existing properties ...
  updateProfile: (displayName?: string, email?: string) => Promise<void>;
}

// Inside the store:
updateProfile: async (displayName?: string, email?: string) => {
  const response = await api.put('/api/auth/profile', { displayName, email });
  
  // Update local user state with new values
  set((state) => ({
    user: state.user ? {
      ...state.user,
      displayName: response.data.displayName ?? state.user.displayName,
    } : null,
  }));
},
```

Example usage in a settings page:

```tsx
import { useState } from 'react';
import { useAuth } from '../hooks/useAuth';

function ProfileSettings() {
  const { user, updateProfile } = useAuth();
  const [displayName, setDisplayName] = useState(user?.displayName || '');
  const [email, setEmail] = useState(user?.email || '');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMessage('');

    try {
      await updateProfile(displayName, email);
      setMessage('Profile updated successfully');
    } catch (error) {
      setMessage('Failed to update profile');
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <h2>Profile Settings</h2>
      
      {message && <div className="message">{message}</div>}

      <div className="form-group">
        <label>Username (cannot be changed)</label>
        <input type="text" value={user?.username} disabled />
      </div>

      <div className="form-group">
        <label>Display Name</label>
        <input
          type="text"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
        />
      </div>

      <div className="form-group">
        <label>Email</label>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
      </div>

      <button type="submit" disabled={saving}>
        {saving ? 'Saving...' : 'Save Changes'}
      </button>
    </form>
  );
}
```

---

## Phase 9: Optional Authentication Bypass

For development or internal tools, you might want to disable authentication entirely via an environment variable.

### Step 9.1: Create Auth Bypass Middleware

Create `Middleware/AuthBypassMiddleware.cs`:

```csharp
using System.Security.Claims;
using Api.Data;

namespace Api.Middleware;

public class AuthBypassMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthBypassMiddleware> _logger;

    public AuthBypassMiddleware(RequestDelegate next, ILogger<AuthBypassMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Create a fake admin identity
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "bypass-user-id"),
            new(ClaimTypes.Name, "bypass-admin"),
            new(ClaimTypes.Role, DbSeeder.Roles.Admin),
            new(ClaimTypes.Role, DbSeeder.Roles.Reader),
        };

        var identity = new ClaimsIdentity(claims, "AuthBypass");
        context.User = new ClaimsPrincipal(identity);

        _logger.LogWarning("Authentication bypassed - running as admin user");

        await _next(context);
    }
}
```

### Step 9.2: Create Extension Method

Create `Extensions/AuthBypassExtensions.cs`:

```csharp
using Api.Middleware;

namespace Api.Extensions;

public static class AuthBypassExtensions
{
    public static IApplicationBuilder UseAuthBypassIfEnabled(
        this IApplicationBuilder app, 
        IConfiguration configuration)
    {
        var bypassAuth = configuration.GetValue<bool>("Auth:Bypass");
        
        if (bypassAuth)
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<AuthBypassMiddleware>>();
            logger.LogWarning("⚠️  AUTHENTICATION IS DISABLED - Auth:Bypass is set to true");
            
            app.UseMiddleware<AuthBypassMiddleware>();
        }
        
        return app;
    }
}
```

### Step 9.3: Update Program.cs

```csharp
using Api.Extensions;

// ... existing code ...

var app = builder.Build();

// Seed database
await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Add auth bypass BEFORE authentication middleware
app.UseAuthBypassIfEnabled(builder.Configuration);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Step 9.4: Update Docker Compose

```yaml
services:
  api:
    # ... existing config ...
    environment:
      # ... existing env vars ...
      - Auth__Bypass=false  # Set to true to disable authentication
```

### Step 9.5: Usage

To disable authentication:

```bash
# Via environment variable
Auth__Bypass=true dotnet run

# Or in docker-compose.yml
environment:
  - Auth__Bypass=true

# Or in appsettings.Development.json
{
  "Auth": {
    "Bypass": true
  }
}
```

When enabled, all requests will automatically be authenticated as an admin user. A warning will be logged on every request.

### Step 9.6: Update React to Handle Auth Bypass (Optional)

If you want the frontend to also know auth is disabled, add an endpoint:

```csharp
// In AuthController.cs
[HttpGet("status")]
[AllowAnonymous]
public IActionResult GetAuthStatus()
{
    var bypassEnabled = _configuration.GetValue<bool>("Auth:Bypass");
    
    return Ok(new 
    { 
        authEnabled = !bypassEnabled,
        bypassEnabled 
    });
}
```

Then in your React app, you can check this on startup and skip the login page if auth is bypassed.

---

## Phase 10: Testing

### Step 10.1: Test Local Authentication

```bash
# Start services
docker compose up -d

# Login with default admin
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'

# Save the token and test protected endpoint
TOKEN="your-token-here"
curl http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer $TOKEN"
```

### Step 10.2: Test Token Refresh

```bash
# Login and get tokens
RESPONSE=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}')

ACCESS_TOKEN=$(echo $RESPONSE | jq -r '.accessToken')
REFRESH_TOKEN=$(echo $RESPONSE | jq -r '.refreshToken')

# Use refresh token to get new access token
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"
```

### Step 10.3: Test Server-side Logout

```bash
# Logout (revokes refresh token)
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"

# Try to use the old refresh token (should fail)
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"
# Returns: {"message":"Refresh token expired or revoked"}
```

### Step 10.4: Test OIDC Provider Configuration

```bash
# Create an OIDC provider (as admin)
curl -X POST http://localhost:5000/api/admin/oidc-providers \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "authentik",
    "displayName": "Login with Authentik",
    "authority": "https://auth.example.com/application/o/myapp/",
    "clientId": "your-client-id",
    "clientSecret": "your-client-secret",
    "enabled": true,
    "autoCreateUsers": true,
    "defaultRole": "Reader"
  }'

# List providers
curl http://localhost:5000/api/auth/oidc/providers
```

### Step 10.5: Configure Authentik

In Authentik:

1. Create a new **OAuth2/OIDC Provider**
2. Set the redirect URI: `http://localhost:5000/api/auth/oidc/authentik/callback`
3. Enable scopes: `openid`, `profile`, `email`
4. Copy the Client ID and Client Secret to your API configuration

### Step 10.6: Test Auth Bypass

```bash
# Start the API with auth bypass enabled
docker compose down
docker compose up -d

# Or temporarily set the env var
Auth__Bypass=true dotnet run

# All endpoints should work without authentication
curl http://localhost:5000/api/auth/me
# Returns the bypass user info

curl http://localhost:5000/api/data
# Works without token

# Check auth status
curl http://localhost:5000/api/auth/status
# Returns: {"authEnabled":false,"bypassEnabled":true}
```

---

## Appendix: UI Settings for OIDC

### Required Admin UI Fields for OIDC Provider Configuration

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| **Name** | text | ✅ | Internal identifier (slug), e.g., `authentik` |
| **Display Name** | text | ✅ | Shown on login button |
| **Authority** | url | ✅ | OIDC issuer URL |
| **Client ID** | text | ✅ | OAuth client ID |
| **Client Secret** | password | ✅ | OAuth client secret (masked) |
| **Enabled** | toggle | | Enable/disable provider |
| **Auto-create Users** | toggle | | Create local accounts automatically |
| **Default Role** | select | | Role for auto-created users |
| **Username Claim** | text | | Claim for username (default: `preferred_username`) |
| **Email Claim** | text | | Claim for email (default: `email`) |
| **Display Name Claim** | text | | Claim for display name (default: `name`) |
| **Roles Claim** | text | | Optional: sync roles from IdP |
| **Icon URL** | url | | Provider logo URL |
| **Button Color** | color | | Hex color for button |

### Example Authentik Configuration

```
Name:           authentik
Display Name:   Login with Authentik
Authority:      https://auth.example.com/application/o/myapp/
Client ID:      abc123
Client Secret:  ***********
Auto-create:    ✅ Enabled
Default Role:   Reader
```

---

## Quick Reference

### Default Credentials

- **Username**: `admin`
- **Password**: `admin`

⚠️ **Change the default password immediately in production!**

### API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | - | Local login |
| POST | `/api/auth/refresh` | - | Refresh access token |
| POST | `/api/auth/logout` | User | Server-side logout |
| POST | `/api/auth/register` | Admin | Register new user |
| POST | `/api/auth/reset-password/{userId}` | Admin | Reset user password (logs temp password) |
| GET | `/api/auth/me` | User | Get current user (basic info) |
| GET | `/api/auth/profile` | User | Get full profile (incl. login methods) |
| PUT | `/api/auth/profile` | User | Update display name / email |
| GET | `/api/auth/status` | - | Check if auth is enabled |
| POST | `/api/auth/change-password` | User | Change password |
| GET | `/api/auth/oidc/providers` | - | List OIDC providers |
| GET | `/api/auth/oidc/{provider}/login` | - | Initiate OIDC login |
| GET | `/api/auth/oidc/{provider}/callback` | - | OIDC callback |
| GET | `/api/admin/users` | Admin | List all users |
| GET | `/api/admin/users/{id}` | Admin | Get user details |
| PUT | `/api/admin/users/{id}/roles` | Admin | Update user roles |
| DELETE | `/api/admin/users/{id}` | Admin | Delete user |
| GET | `/api/admin/oidc-providers` | Admin | List all providers |
| POST | `/api/admin/oidc-providers` | Admin | Create provider |
| PUT | `/api/admin/oidc-providers/{id}` | Admin | Update provider |
| DELETE | `/api/admin/oidc-providers/{id}` | Admin | Delete provider |

### Docker Commands

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f api

# Rebuild after changes
docker compose up -d --build

# Stop all services
docker compose down

# Reset database
docker compose down -v
docker compose up -d
```
