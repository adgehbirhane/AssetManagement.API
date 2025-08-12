using AssetManagement.API.Models;
using System.Security.Claims;

namespace AssetManagement.API.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        bool ValidateToken(string token);
        ClaimsPrincipal? GetPrincipalFromToken(string token);
    }
}
