# Asset Management API

A professional .NET 8 Web API backend for the Employee Asset Management System, built with Entity Framework Core, PostgreSQL, and JWT authentication.

## Features

- **Authentication & Authorization**: JWT-based authentication with role-based access control (User/Admin)
- **Asset Management**: Full CRUD operations for company assets
- **Asset Requests**: Users can request assets, admins can approve/reject
- **User Management**: User registration, login, and profile management
- **Database**: PostgreSQL with Entity Framework Core
- **API Documentation**: Swagger/OpenAPI integration
- **CORS**: Configured for React frontend
- **AutoMapper**: Object-to-object mapping
- **Password Security**: SHA256 hashing

## Technology Stack

- **.NET 8**
- **Entity Framework Core 8**
- **PostgreSQL**
- **JWT Authentication**
- **AutoMapper**
- **Swagger/OpenAPI**

## Prerequisites

- .NET 8 SDK
- PostgreSQL database
- Visual Studio 2022 or VS Code

## Setup Instructions

### 1. Database Setup

1. Install PostgreSQL if not already installed
2. Create a new database:
   ```sql
   CREATE DATABASE asset_mgt_db;
   ```
3. Note down your PostgreSQL credentials (username, password, port)

### 2. JWT Configuration

Update the JWT secret in `appsettings.json` and `appsettings.Development.json`:

```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-with-at-least-32-characters",
    "Issuer": "AssetManagementAPI",
    "Audience": "AssetManagementClient"
  }
}
```

**Important**: Use a strong, unique secret key in production.

### 3. Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=asset_mgt_db;Username=postgres;Password=your_password;SSL Mode=Prefer;Trust Server Certificate=true;"
  }
}
```

Replace `your_password` with your actual PostgreSQL password.

## How to Run

### Using Visual Studio

1. Open the solution in Visual Studio 2022
2. Set `AssetManagement.API` as the startup project
3. Press F5 or click "Start Debugging"

### Using Command Line

```bash
cd AssetManagement.API
dotnet run
```

The API will be available at:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001
- **Swagger UI**: https://localhost:5001/swagger

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `GET /api/auth/me` - Get current user (authenticated)

### Assets
- `GET /api/assets` - Get all assets (authenticated)
- `GET /api/assets/{id}` - Get asset by ID (authenticated)
- `POST /api/assets` - Create asset (admin only)
- `PUT /api/assets/{id}` - Update asset (admin only)
- `DELETE /api/assets/{id}` - Delete asset (admin only)

### Asset Requests
- `GET /api/asset-requests` - Get asset requests (authenticated)
- `POST /api/asset-requests` - Create asset request (authenticated)
- `GET /api/asset-requests/{id}` - Get asset request by ID (authenticated)
- `PUT /api/asset-requests/{id}` - Update asset request status (admin only)

### Users
- `GET /api/users` - Get all users (admin only)
- `GET /api/users/{id}` - Get user by ID (admin only)

### Debug & Testing
- `GET /api/auth/test` - Test database connection and DateTime handling
- `GET /api/auth/debug` - Debug authentication and database status (authenticated)

## Demo Credentials

The application comes with pre-seeded demo data:

### Admin User
- **Email**: admin@company.com
- **Password**: admin123
- **Role**: Admin

### Regular User
- **Email**: user@company.com
- **Password**: user123
- **Role**: User

## Sample Assets

The database is seeded with 7 sample assets:
- MacBook Pro 16-inch (Laptop)
- Dell XPS 13 (Laptop)
- iPhone 15 Pro (Phone)
- Samsung Galaxy S24 (Phone)
- Dell UltraSharp 27-inch Monitor (Monitor)
- iPad Pro 12.9-inch (Tablet)
- HP EliteDesk Desktop (Desktop)

## API Response Format

All API responses follow a consistent format:

```json
{
  "success": true,
  "data": { /* response data */ },
  "message": "Success message"
}
```

Error responses:
```json
{
  "success": false,
  "message": "Error message"
}
```

## Authentication

The API uses JWT (JSON Web Tokens) for authentication:

1. **Login/Register** to get a token
2. **Include token** in subsequent requests:
   ```
   Authorization: Bearer <your-token>
   ```
3. **Token expires** after 7 days

## CORS Configuration

The API is configured to allow requests from:
- http://localhost:5173 (Vite default)
- http://localhost:3000 (Create React App default)

## Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Verify PostgreSQL is running
   - Check connection string in `appsettings.json`
   - Ensure database exists
   - Verify username/password

2. **DateTime Errors**
   - The API automatically handles DateTime conversion to UTC
   - If you see DateTime errors, try the test endpoint: `GET /api/auth/test`

3. **Authentication Issues**
   - Check JWT secret configuration
   - Verify token is included in Authorization header
   - Use debug endpoint: `GET /api/auth/debug`

4. **Frontend Connection Issues**
   - Verify API URL in frontend configuration
   - Check CORS settings
   - Ensure API is running on correct port

### Debug Endpoints

Use these endpoints to troubleshoot:

- **`GET /api/auth/test`** - Tests database connection and DateTime handling
- **`GET /api/auth/debug`** - Shows authentication status and database counts (requires authentication)

### Database Reset

To reset the database and re-seed data:
1. Stop the application
2. Delete the database: `DROP DATABASE asset_mgt_db;`
3. Recreate the database: `CREATE DATABASE asset_mgt_db;`
4. Restart the application

## Development

### Project Structure

```
AssetManagement.API/
├── Controllers/          # API endpoints
├── Data/                # Database context and seeding
├── DTOs/                # Data transfer objects
├── Mapping/             # AutoMapper profiles
├── Models/              # Entity models
├── Services/            # Business logic services
└── Program.cs           # Application entry point
```

### Adding New Features

1. **New Entity**: Add model in `Models/` folder
2. **Database**: Update `ApplicationDbContext`
3. **DTOs**: Add request/response DTOs
4. **Mapping**: Configure AutoMapper mappings
5. **Controller**: Create API endpoints
6. **Testing**: Use Swagger UI or debug endpoints

## Production Considerations

1. **Security**:
   - Use strong JWT secrets
   - Enable HTTPS
   - Configure proper CORS origins
   - Use environment variables for secrets

2. **Database**:
   - Use connection pooling
   - Configure proper indexes
   - Set up database migrations

3. **Performance**:
   - Enable response compression
   - Configure caching
   - Use async/await patterns

4. **Monitoring**:
   - Add logging
   - Configure health checks
   - Set up application insights

## License

This project is for educational purposes. Feel free to use and modify as needed.
