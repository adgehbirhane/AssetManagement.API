using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Enums;
using AssetManagement.API.Interfaces;
using AssetManagement.API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace AssetManagement.API.Repositories;

public class AssetsRepository : IAssetsRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AssetsRepository(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AssetListResponse> GetAssets(AssetQueryParameters parameters)
    {
        var query = _context.Assets
            .Include(a => a.Category)
            .Include(a => a.AssignedTo)
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        var assetDtos = _mapper.Map<List<AssetDto>>(assets);

        return new AssetListResponse
        {
            Data = assetDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        };
    }

    public async Task<ApiResponse<AssetDto>> GetAsset(Guid id)
    {
        var asset = await _context.Assets
            .Include(a => a.Category)
            .Include(a => a.AssignedTo)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asset == null)
        {
            return new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Asset not found"
            };
        }

        var assetDto = _mapper.Map<AssetDto>(asset);
        return new ApiResponse<AssetDto>
        {
            Data = assetDto,
            Message = "Asset retrieved successfully"
        };
    }

    public async Task<ApiResponse<AssetDto>> CreateAsset(CreateAssetRequest request, IFormFile? image)
    {
        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Invalid category"
            };
        }

        var existingAsset = await _context.Assets
            .FirstOrDefaultAsync(a => a.SerialNumber == request.SerialNumber);

        if (existingAsset != null)
        {
            return new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "An asset with this serial number already exists"
            };
        }

        string? imageUrl = null;
        if (image != null)
        {
            imageUrl = await SaveAssetImageAsync(image);
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

        await _context.Entry(asset)
            .Reference(a => a.Category)
            .LoadAsync();

        var assetDto = _mapper.Map<AssetDto>(asset);
        return new ApiResponse<AssetDto>
        {
            Data = assetDto,
            Message = "Asset created successfully"
        };
    }

    public async Task<ApiResponse<AssetDto>> UpdateAsset(Guid id, UpdateAssetRequest request, IFormFile? image)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Asset not found"
            };
        }

        if (!string.IsNullOrEmpty(request.SerialNumber) &&
            request.SerialNumber != asset.SerialNumber &&
            await _context.Assets.AnyAsync(a => a.SerialNumber == request.SerialNumber))
        {
            return new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Serial number already exists"
            };
        }

        if (!string.IsNullOrEmpty(request.Name)) asset.Name = request.Name;
        if (request.CategoryId.HasValue) asset.CategoryId = request.CategoryId.Value;
        if (!string.IsNullOrEmpty(request.SerialNumber)) asset.SerialNumber = request.SerialNumber;
        if (request.PurchaseDate.HasValue) asset.PurchaseDate = request.PurchaseDate.Value;

        if (image != null)
        {
            if (!string.IsNullOrEmpty(asset.ImageUrl))
            {
                DeleteAssetImage(asset.ImageUrl);
            }
            asset.ImageUrl = await SaveAssetImageAsync(image);
        }

        asset.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var assetDto = _mapper.Map<AssetDto>(asset);
        return new ApiResponse<AssetDto>
        {
            Data = assetDto,
            Message = "Asset updated successfully"
        };
    }

    public async Task<ApiResponse<AssetDto>> UpdateAssetStatus(Guid id, UpdateAssetStatusRequest request)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Asset not found"
            };
        }

        if (!Enum.TryParse<AssetStatus>(request.Status, out var newStatus))
        {
            return new ApiResponse<AssetDto>
            {
                Success = false,
                Message = "Invalid status value"
            };
        }

        asset.Status = newStatus;
        asset.UpdatedAt = DateTime.UtcNow;

        if (newStatus == AssetStatus.Available)
        {
            asset.AssignedToId = null;
            asset.AssignedAt = null;
        }

        await _context.SaveChangesAsync();

        await _context.Entry(asset)
            .Reference(a => a.Category)
            .LoadAsync();

        var assetDto = _mapper.Map<AssetDto>(asset);
        return new ApiResponse<AssetDto>
        {
            Data = assetDto,
            Message = "Asset status updated successfully"
        };
    }

    public async Task<ApiResponse<object>> DeleteAsset(Guid id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Asset not found"
            };
        }

        if (!string.IsNullOrEmpty(asset.ImageUrl))
        {
            DeleteAssetImage(asset.ImageUrl);
        }

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();

        return new ApiResponse<object>
        {
            Message = "Asset deleted successfully"
        };
    }

    public async Task<string> SaveAssetImageAsync(IFormFile imageFile)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Invalid file type. Only image files are allowed.");
        }

        if (imageFile.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("File size cannot exceed 5MB.");
        }

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assets");
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        return fileName;
    }

    public IActionResult GetImage(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new BadRequestObjectResult(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid image file name"
            });
        }

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assets", fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return new NotFoundObjectResult(new ApiResponse<object>
            {
                Success = false,
                Message = "Image not found"
            });
        }

        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new PhysicalFileResult(filePath, contentType);
    }

    public void DeleteAssetImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assets", fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    private IQueryable<Asset> ApplyFilters(IQueryable<Asset> query, AssetQueryParameters parameters)
    {
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

        return query;
    }
}