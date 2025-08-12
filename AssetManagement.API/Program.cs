using AssetManagement.API.Data;
using AssetManagement.API.Interfaces;
using AssetManagement.API.Mapping;
using AssetManagement.API.Repositories;
using AssetManagement.API.Services;
using AssetManagement.API.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

Console.WriteLine($"JWT Configuration - Secret Length: {key.Length}, Issuer: {jwtSettings["Issuer"]}, Audience: {jwtSettings["Audience"]}");
Console.WriteLine($"JWT Key (first 10 chars): {Convert.ToBase64String(key.Take(10).ToArray())}");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    // Add event handlers for debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            Console.WriteLine($"Exception type: {context.Exception.GetType().Name}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {context.Exception.InnerException.Message}");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userEmail = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            Console.WriteLine($"JWT Token validated successfully for user: {userEmail} (ID: {userId})");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge issued: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"JWT Token received: {token.Substring(0, Math.Min(50, token.Length))}...");
            }
            return Task.CompletedTask;
        }
    };
});

// Configure Authorization
builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Add repositories
builder.Services.AddScoped<IAssetRequestsRepository, AssetRequestsRepository>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { 
        Title = "Asset Management API", 
        Version = "v1",
        Description = "API for managing assets, users, and asset requests",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Asset Management Team",
            Email = "support@assetmanagement.com"
        }
    });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    
    // Global security requirement removed - let SecurityRequirementsOperationFilter handle it
    
    // Add operation filter to show lock icon on protected endpoints
    c.OperationFilter<SecurityRequirementsOperationFilter>();
    
    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // Customize operation IDs
    c.CustomOperationIds(apiDesc =>
    {
        return apiDesc.TryGetMethodInfo(out var methodInfo) ? methodInfo.Name : null;
    });
    
    // Add tags for better organization
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((name, api) => true);
});

var app = builder.Build();

// Debug: Log the current environment and configuration
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Content Root: {app.Environment.ContentRootPath}");
Console.WriteLine($"Web Root: {app.Environment.WebRootPath}");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Asset Management API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Asset Management API Documentation";
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(3);
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        
        // Custom styling removed for simplicity
    });
}

app.UseHttpsRedirection();

// Add static files middleware to serve uploaded images
app.UseStaticFiles();

// Use CORS
app.UseCors("AllowReactApp");
Console.WriteLine("CORS middleware added");

app.UseAuthentication();
Console.WriteLine("Authentication middleware added");

app.UseAuthorization();
Console.WriteLine("Authorization middleware added");

app.MapControllers();
Console.WriteLine("Controllers mapped");

// Debug: List all registered endpoints
var endpoints = app.Urls.ToList();
Console.WriteLine($"Application URLs: {string.Join(", ", endpoints)}");

// Initialize and update database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    
    try
    {
        Console.WriteLine("Starting database initialization...");
        
        // Apply any pending migrations
        await context.Database.MigrateAsync();
        Console.WriteLine("Database migrations applied successfully");
        
        // Seed data if database is empty
        var hasUsers = await context.Users.AnyAsync();
        Console.WriteLine($"Database has users: {hasUsers}");
        
        if (!hasUsers)
        {
            await DbSeeder.SeedData(context, passwordService);
            Console.WriteLine("Database seeded with initial data!");
        }
        else
        {
            Console.WriteLine("Database already contains data, skipping seeding.");
        }
        
        Console.WriteLine("Database initialization completed successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        throw;
    }
}

Console.WriteLine("Application startup completed successfully!");

app.Run();
