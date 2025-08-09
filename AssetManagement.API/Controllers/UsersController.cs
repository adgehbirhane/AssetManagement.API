using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AutoMapper;
using System.IO;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UsersController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers([FromQuery] QueryParameters parameters)
    {
        var query = _context.Users.AsQueryable();

        // Apply search filter
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

        return Ok(new PaginatedResponse<UserDto>
        {
            Data = userDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found"
            });
        }

        var userDto = _mapper.Map<UserDto>(user);

        return Ok(new ApiResponse<UserDto>
        {
            Data = userDto,
            Message = "User retrieved successfully"
        });
    }

    [HttpPost("{id}/profile-image")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UploadProfileImage(Guid id, IFormFile imageFile)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Invalid file type. Only image files are allowed."
                });
            }

            // Validate file size (5MB max)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "File size cannot exceed 5MB."
                });
            }

            // Delete old profile image if it exists
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                DeleteProfileImage(user.ProfileImageUrl);
            }

            // Save new profile image
            var fileName = await SaveProfileImageAsync(imageFile);
            
            // Update user profile image URL
            user.ProfileImageUrl = fileName;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);

            return Ok(new ApiResponse<UserDto>
            {
                Data = userDto,
                Message = "Profile image uploaded successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = $"Failed to upload profile image: {ex.Message}"
            });
        }
    }

    [HttpGet("{id}/profile-image")]
    [AllowAnonymous]
    public IActionResult GetProfileImage(Guid id)
    {
        try
        {
            var user = _context.Users
                .Where(u => u.Id == id)
                .Select(u => new { u.Id, u.ProfileImageUrl })
                .FirstOrDefault();

            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            if (string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Profile image not found"
                });
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", user.ProfileImageUrl);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Profile image file not found"
                });
            }

            var contentType = GetContentType(filePath);
            return PhysicalFile(filePath, contentType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"Failed to retrieve profile image: {ex.Message}"
            });
        }
    }

    [HttpDelete("{id}/profile-image")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProfileImage(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            if (string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User does not have a profile image"
                });
            }

            // Delete the image file
            DeleteProfileImage(user.ProfileImageUrl);

            // Update user profile image URL
            user.ProfileImageUrl = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Message = "Profile image deleted successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"Failed to delete profile image: {ex.Message}"
            });
        }
    }

    private async Task<string> SaveProfileImageAsync(IFormFile imageFile)
    {
        try
        {
            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Return filename
            return fileName;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save profile image: {ex.Message}");
        }
    }

    private void DeleteProfileImage(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            // Convert URL to file path
            var fileName = Path.GetFileName(imageUrl);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception)
        {
            // Log the error but don't throw - image deletion failure shouldn't prevent user update
            // You might want to use a proper logging framework here
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
