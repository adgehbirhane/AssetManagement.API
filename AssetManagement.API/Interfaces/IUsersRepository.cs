using AssetManagement.API.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetManagement.API.Interfaces;

public interface IUsersRepository
{
    Task<PaginatedResponse<UserDto>> GetUsers(QueryParameters parameters);
    Task<ApiResponse<UserDto>> GetUser(Guid id);
    Task<ApiResponse<UserDto>> UpdateUser(Guid id, UpdateProfileRequest request, ClaimsPrincipal user);
    Task<ApiResponse<UserDto>> UploadProfileImage(Guid id, IFormFile imageFile, ClaimsPrincipal user);
    Task<ApiResponse<object>> DeleteProfileImage(Guid id, ClaimsPrincipal user);
    IActionResult GetProfileImage(Guid id);
}