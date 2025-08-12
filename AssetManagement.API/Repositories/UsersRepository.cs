using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Interfaces;
using AssetManagement.API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;

namespace AssetManagement.API.Repositories;

public class UsersRepository : IUsersRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UsersRepository(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<UserDto>> GetUsers(QueryParameters parameters)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(parameters.Search))
        {
            query = query.Where(u =>
                u.FirstName.Contains(parameters.Search) ||
                u.LastName.Contains(parameters.Search) ||
                u.Email.Contains(parameters.Search));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        var userDtos = _mapper.Map<List<UserDto>>(users);

        return new PaginatedResponse<UserDto>
        {
            Data = userDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        };
    }

    public async Task<ApiResponse<UserDto>> GetUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found"
            };
        }

        var userDto = _mapper.Map<UserDto>(user);
        return new ApiResponse<UserDto>
        {
            Data = userDto,
            Message = "User retrieved successfully"
        };
    }

    public async Task<ApiResponse<UserDto>> UpdateUser(Guid id, UpdateProfileRequest request, ClaimsPrincipal user)
    {
        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user.IsInRole("Admin");

        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Invalid or expired token"
            };
        }

        if (!isAdmin && currentUserGuid != id)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Forbidden"
            };
        }

        var userToUpdate = await _context.Users.FindAsync(id);
        if (userToUpdate == null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found"
            };
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != userToUpdate.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }
        }

        if (!string.IsNullOrEmpty(request.FirstName))
            userToUpdate.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName))
            userToUpdate.LastName = request.LastName;
        if (!string.IsNullOrEmpty(request.Email))
            userToUpdate.Email = request.Email;

        userToUpdate.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var userDto = _mapper.Map<UserDto>(userToUpdate);
        return new ApiResponse<UserDto>
        {
            Data = userDto,
            Message = "Profile updated successfully"
        };
    }

    public async Task<ApiResponse<UserDto>> UploadProfileImage(Guid id, IFormFile imageFile, ClaimsPrincipal user)
    {
        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user.IsInRole("Admin");

        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Invalid or expired token"
            };
        }

        if (!isAdmin && currentUserGuid != id)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Forbidden"
            };
        }

        var userToUpdate = await _context.Users.FindAsync(id);
        if (userToUpdate == null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found"
            };
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Invalid file type. Only image files are allowed."
            };
        }

        if (imageFile.Length > 5 * 1024 * 1024)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "File size cannot exceed 5MB."
            };
        }

        if (!string.IsNullOrEmpty(userToUpdate.ProfileImageUrl))
        {
            DeleteProfileImage(userToUpdate.ProfileImageUrl);
        }

        var fileName = await SaveProfileImageAsync(imageFile);

        userToUpdate.ProfileImageUrl = fileName;
        userToUpdate.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var userDto = _mapper.Map<UserDto>(userToUpdate);
        return new ApiResponse<UserDto>
        {
            Data = userDto,
            Message = "Profile image uploaded successfully"
        };
    }

    public IActionResult GetProfileImage(Guid id)
    {
        var user = _context.Users
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.ProfileImageUrl })
            .FirstOrDefault();

        if (user == null)
        {
            return new NotFoundObjectResult(new ApiResponse<object>
            {
                Success = false,
                Message = "User not found"
            });
        }

        if (string.IsNullOrEmpty(user.ProfileImageUrl))
        {
            return new NotFoundObjectResult(new ApiResponse<object>
            {
                Success = false,
                Message = "Profile image not found"
            });
        }

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", user.ProfileImageUrl);

        if (!System.IO.File.Exists(filePath))
        {
            return new NotFoundObjectResult(new ApiResponse<object>
            {
                Success = false,
                Message = "Profile image file not found"
            });
        }

        var contentType = GetContentType(filePath);
        return new PhysicalFileResult(filePath, contentType);
    }

    public async Task<ApiResponse<object>> DeleteProfileImage(Guid id, ClaimsPrincipal user)
    {
        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = user.IsInRole("Admin");

        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserGuid))
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid or expired token"
            };
        }

        if (!isAdmin && currentUserGuid != id)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Forbidden"
            };
        }

        var userToUpdate = await _context.Users.FindAsync(id);
        if (userToUpdate == null)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "User not found"
            };
        }

        if (string.IsNullOrEmpty(userToUpdate.ProfileImageUrl))
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "User does not have a profile image"
            };
        }

        DeleteProfileImage(userToUpdate.ProfileImageUrl);

        userToUpdate.ProfileImageUrl = null;
        userToUpdate.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ApiResponse<object>
        {
            Message = "Profile image deleted successfully"
        };
    }

    private async Task<string> SaveProfileImageAsync(IFormFile imageFile)
    {
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        return fileName;
    }

    private void DeleteProfileImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    private string GetContentType(string path)
    {
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
}