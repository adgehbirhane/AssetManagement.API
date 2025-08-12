using AssetManagement.API.DTOs;
using System.Security.Claims;

namespace AssetManagement.API.Interfaces;

public interface IAuthRepository
{
    Task<ApiResponse<AuthResponse>> Login(LoginRequest request);
    Task<ApiResponse<AuthResponse>> Register(RegisterRequest request);
    Task<ApiResponse<UserDto>> GetCurrentUser(ClaimsPrincipal user);
}