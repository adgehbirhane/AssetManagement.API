# Asset Management API

.NET 8 Web API backend for the Employee Asset Management System, built with Entity Framework Core, PostgreSQL, and JWT authentication.

## Features

- **Authentication & Authorization**: JWT-based authentication with role-based access control (User/Admin)
- **Asset Management**: Full CRUD operations for company assets with image support
- **Asset Requests**: Users can request assets, admins can approve/reject
- **User Management**: User registration, login, and profile management with profile images
- **Image Management**: Image upload, storage, and serving for assets and user profiles
- **Database**: PostgreSQL with Entity Framework Core and automatic migrations
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
### 3. Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=asset_mgt_db;Username=postgres;Password=your_password;SSL Mode=Prefer;Trust Server Certificate=true;"
  }
}
```

### 4. Database Migrations

The application uses Entity Framework Core migrations for database schema management. After making changes to models, you need to create and apply migrations.

#### Install Entity Framework Tools (if not already installed)
```bash
dotnet tool install --global dotnet-ef
```

#### Create a New Migration
```bash
# Navigate to the API project directory
cd AssetManagement.API

# Create a new migration
dotnet ef migrations add MigrationName

# Examples:
dotnet ef migrations add AddProfileImageUrlToUser
dotnet ef migrations add AddNewFeature
dotnet ef migrations add UpdateAssetModel
```

#### Apply Migrations to Database
```bash
# Apply all pending migrations
dotnet ef database update

# Apply to a specific migration
dotnet ef database update MigrationName

# Apply to the latest migration
dotnet ef database update
```

#### Migration Management Commands
```bash
# List all migrations
dotnet ef migrations list

# Remove the last migration (if not applied to database)
dotnet ef migrations remove

# Generate SQL script for migrations (without applying)
dotnet ef migrations script

# Generate SQL script from specific migration to latest
dotnet ef migrations script FromMigrationName

# Update database to a specific migration
dotnet ef database update MigrationName

# Rollback to a previous migration
dotnet ef database update PreviousMigrationName
```

#### Common Migration Scenarios

**Adding a New Column:**
```bash
# 1. Update your model (e.g., add ProfileImageUrl to User)
# 2. Create migration
dotnet ef migrations add AddProfileImageUrlToUser
# 3. Apply migration
dotnet ef database update
```

**Updating Existing Models:**
```bash
# 1. Modify your model properties
# 2. Create migration
dotnet ef migrations add UpdateUserModel
# 3. Apply migration
dotnet ef database update
```

**Adding New Entities:**
```bash
# 1. Create new model class
# 2. Add to DbContext
# 3. Create migration
dotnet ef migrations add AddNewEntity
# 4. Apply migration
dotnet ef database update
```

## How to Run

**Clone the repository**
   ```bash
   git clone https://github.com/adgehbirhane/AssetManagement.API.git
   ```

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
- `GET /api/assets/images/{fileName}` - Get asset image (public)

### Asset Requests
- `GET /api/asset-requests` - Get asset requests (authenticated)
- `POST /api/asset-requests` - Create asset request (authenticated)
- `GET /api/asset-requests/{id}` - Get asset request by ID (authenticated)
- `PUT /api/asset-requests/{id}` - Update asset request status (admin only)

### Users
- `GET /api/users` - Get all users (admin only)
- `GET /api/users/{id}` - Get user by ID (admin only)
- `POST /api/users/{id}/profile-image` - Upload user profile image (admin only)
- `GET /api/users/{id}/profile-image` - Get user profile image (public)
- `DELETE /api/users/{id}/profile-image` - Delete user profile image (admin only)

## Demo Credentials

The application comes with pre-seeded demo data:

### Admin User
- **Email**: admin@gmail.com
- **Password**: Admin@123
- **Role**: Admin

### Regular User
- **Email**: user@gmail.com
- **Password**: UserW123
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

4. **Frontend Connection Issues**
   - Verify API URL in frontend configuration
   - Check CORS settings
   - Ensure API is running on correct port

5. **Migration Issues**
   - Ensure Entity Framework tools are installed: `dotnet tool install --global dotnet-ef`
   - Stop the API before running migrations to avoid file locks
   - Check for build errors before creating migrations
   - Verify database connection before applying migrations
   - Use `dotnet ef migrations list` to check migration status

### Database Reset

To reset the database and re-seed data:
1. Stop the application
2. Delete the database: `DROP DATABASE asset_mgt_db;`
3. Recreate the database: `CREATE DATABASE asset_mgt_db;`
4. Restart the application


### Adding New Features

1. **New Entity**: Add model in `Models/` folder
2. **Database**: Update `ApplicationDbContext`
3. **DTOs**: Add request/response DTOs
4. **Mapping**: Configure AutoMapper mappings
5. **Controller**: Create API endpoints
6. **Testing**: Use Swagger UI or debug endpoints
