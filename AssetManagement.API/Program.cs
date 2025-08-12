using AssetManagement.API.Data;
using AssetManagement.API.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Register services
ServiceConfiguration.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
MiddlewareConfiguration.ConfigureMiddleware(app, app.Environment);

// Initialize database
await DbConfig.InitializeDatabase(app);

app.Run();