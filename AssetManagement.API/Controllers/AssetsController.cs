using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Models;
using AssetManagement.API.Enums;
using AutoMapper;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AssetsController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<AssetListResponse>> GetAssets([FromQuery] AssetQueryParameters parameters)
    {
        var query = _context.Assets
            .Include(a => a.Category)
            .Include(a => a.AssignedTo)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(parameters.Category))
        {
            query = query.Where(a => a.Category.Name.Contains(parameters.Category));
        }

        if (!string.IsNullOrEmpty(parameters.Status))
        {
            if (Enum.TryParse<AssetStatus>(parameters.Status, out var status))
            {
                query = query.Where(a => a.Status == status);
            }
        }

        if (!string.IsNullOrEmpty(parameters.Search))
        {
            query = query.Where(a => 
                a.Name.Contains(parameters.Search) || 
                a.SerialNumber.Contains(parameters.Search) ||
                a.Category.Name.Contains(parameters.Search));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        // Convert to DTOs with string status values
        var assetDtos = assets.Select(a => new AssetDto
        {
            Id = a.Id,
            Name = a.Name,
            CategoryId = a.CategoryId,
            Category = new CategoryDto
            {
                Id = a.Category.Id,
                Name = a.Category.Name,
                Description = a.Category.Description,
                Status = a.Category.Status.ToString(),
                CreatedAt = a.Category.CreatedAt,
                UpdatedAt = a.Category.UpdatedAt
            },
            SerialNumber = a.SerialNumber,
            PurchaseDate = a.PurchaseDate,
            Status = a.Status.ToString(),
            AssignedToId = a.AssignedToId,
            AssignedTo = a.AssignedTo != null ? new UserDto
            {
                Id = a.AssignedTo.Id,
                Email = a.AssignedTo.Email,
                FirstName = a.AssignedTo.FirstName,
                LastName = a.AssignedTo.LastName,
                Role = a.AssignedTo.Role.ToString(),
                CreatedAt = a.AssignedTo.CreatedAt,
                UpdatedAt = a.AssignedTo.UpdatedAt
            } : null,
            AssignedAt = a.AssignedAt,
            ImageUrl = a.ImageUrl,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();

        return Ok(new AssetListResponse
        {
            Data = assetDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> GetAsset(Guid id)
    {
        var asset = await _context.Assets
            .Include(a => a.Category)
            .Include(a => a.AssignedTo)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asset == null)
        {
            return NotFound(new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Asset not found"
            });
        }

        var assetDto = new AssetDto
        {
            Id = asset.Id,
            Name = asset.Name,
            CategoryId = asset.CategoryId,
            Category = new CategoryDto
            {
                Id = asset.Category.Id,
                Name = asset.Category.Name,
                Description = asset.Category.Description,
                Status = asset.Category.Status.ToString(),
                CreatedAt = asset.Category.CreatedAt,
                UpdatedAt = asset.Category.UpdatedAt
            },
            SerialNumber = asset.SerialNumber,
            PurchaseDate = asset.PurchaseDate,
            Status = asset.Status.ToString(),
            AssignedToId = asset.AssignedToId,
            AssignedTo = asset.AssignedTo != null ? new UserDto
            {
                Id = asset.AssignedTo.Id,
                Email = asset.AssignedTo.Email,
                FirstName = asset.AssignedTo.FirstName,
                LastName = asset.AssignedTo.LastName,
                Role = asset.AssignedTo.Role.ToString(),
                CreatedAt = asset.AssignedTo.CreatedAt,
                UpdatedAt = asset.AssignedTo.UpdatedAt
            } : null,
            AssignedAt = asset.AssignedAt,
            ImageUrl = asset.ImageUrl,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt
        };

        return Ok(new ApiResponse<AssetDto>
        {
            Data = assetDto,
            Message = "Asset retrieved successfully"
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> CreateAsset([FromForm] CreateAssetRequest request)
    {
        // Validate category exists
        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest(new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Invalid category"
            });
        }

        // Check if serial number already exists
        var existingAsset = await _context.Assets
            .FirstOrDefaultAsync(a => a.SerialNumber == request.SerialNumber);

        if (existingAsset != null)
        {
            return BadRequest(new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "An asset with this serial number already exists"
            });
        }

        // Handle image upload if provided
        string? imageUrl = null;
        if (request.Image != null)
        {
            imageUrl = await SaveAssetImageAsync(request.Image);
        }

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CategoryId = request.CategoryId,
            SerialNumber = request.SerialNumber,
            PurchaseDate = request.PurchaseDate,
            Status = AssetStatus.Available,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Load related data for response
        await _context.Entry(asset)
            .Reference(a => a.Category)
            .LoadAsync();

        var assetDto = _mapper.Map<AssetDto>(asset);

        return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, new ApiResponse<AssetDto>
        {
            Data = assetDto,
            Message = "Asset created successfully"
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> UpdateAsset(Guid id, [FromForm] UpdateAssetRequest request)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound(new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Asset not found"
            });
        }

        // Check if serial number is being changed and if it already exists
        if (!string.IsNullOrEmpty(request.SerialNumber) && 
            request.SerialNumber != asset.SerialNumber &&
            await _context.Assets.AnyAsync(a => a.SerialNumber == request.SerialNumber))
        {
            return BadRequest(new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Serial number already exists"
            });
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Name)) asset.Name = request.Name;
        if (request.CategoryId.HasValue) asset.CategoryId = request.CategoryId.Value;
        if (!string.IsNullOrEmpty(request.SerialNumber)) asset.SerialNumber = request.SerialNumber;
        if (request.PurchaseDate.HasValue)
        {
            asset.PurchaseDate = request.PurchaseDate.Value;
        }

        // Handle image update if provided
        if (request.Image != null)
        {
            // Delete old image if exists
            if (!string.IsNullOrEmpty(asset.ImageUrl))
            {
                DeleteAssetImage(asset.ImageUrl);
            }
            
            // Save new image
            asset.ImageUrl = await SaveAssetImageAsync(request.Image);
        }

        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var assetDto = _mapper.Map<AssetDto>(asset);

        return Ok(new ApiResponse<AssetDto>
        {
            Data = assetDto,
            Message = "Asset updated successfully"
        });
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> UpdateAssetStatus(Guid id, [FromBody] UpdateAssetStatusRequest request)
    {

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound(new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Asset not found"
            });
        }

        // Parse and validate status
        if (!Enum.TryParse<AssetStatus>(request.Status, out var newStatus))
        {
            return BadRequest(new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Invalid status value; status has be one of  Available, Assigned, Maintenance, or Retired"
            });
        }

        // Update only the status
        asset.Status = newStatus;
        asset.UpdatedAt = DateTime.UtcNow;

        // If status is Available, clear assignment
        if (newStatus == AssetStatus.Available)
        {
            asset.AssignedToId = null;
            asset.AssignedAt = null;
        }

        await _context.SaveChangesAsync();

        // Load related data for response
        await _context.Entry(asset)
            .Reference(a => a.Category)
            .LoadAsync();

        var assetDto = _mapper.Map<AssetDto>(asset);

        return Ok(new ApiResponse<AssetDto>
        {
            Data = assetDto,
            Message = "Asset status updated successfully"
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsset(Guid id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Asset not found"
            });
        }

        // Delete associated image if exists
        if (!string.IsNullOrEmpty(asset.ImageUrl))
        {
            DeleteAssetImage(asset.ImageUrl);
        }

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>
        {
            Message = "Asset deleted successfully"
        });
    }

    private async Task<string> SaveAssetImageAsync(IFormFile imageFile)
    {
        try
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file type. Only image files are allowed.");
            }

            // Validate file size (5MB max)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("File size cannot exceed 5MB.");
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assets");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Return relative URL
            return $"{fileName}";
        }
        catch (Exception ex)
        {
            // Log the error (you might want to use a proper logging framework)
            throw new InvalidOperationException($"Failed to save image: {ex.Message}");
        }
    }

    [HttpGet("images/{fileName}")]
    [AllowAnonymous]
    public IActionResult GetImage(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid image file name"
            });
        }

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assets", fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Image not found"
            });
        }

        var contentType = GetContentType(filePath);
        return PhysicalFile(filePath, contentType);
    }

    // Helper for content type detection
    private string GetContentType(string path)
    {
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }


    private void DeleteAssetImage(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            // Convert URL to file path
            var fileName = Path.GetFileName(imageUrl);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assets", fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception)
        {
            // Log the error but don't throw - image deletion failure shouldn't prevent asset update
            // You might want to use a proper logging framework here
        }
    }
}
