using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Enums;
using AssetManagement.API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Numerics;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CategoriesController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CategoryDto>>> GetCategories([FromQuery] CategoryQueryParameters parameters)
    {
        var query = _context.Categories.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(parameters.Search))
        {
            var search = parameters.Search.ToLower();

            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                c.Description.ToLower().Contains(search));
        }

        // Filter only active category
        query = query.Where(c => c.Status == CategoryStatus.ACTIVE);

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var categories = await query
            .Include(c => c.Assets)
            .OrderBy(c => c.Name)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        // Convert to DTOs with string status values
        var categoryDtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Status = c.Status.ToString(),
            AssetsCount = c.Assets.Count,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        return Ok(new PaginatedResponse<CategoryDto>
        {
            Data = categoryDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        });
    }

        [HttpGet("all")]
        [Authorize]
        public async Task<ActionResult<PaginatedResponse<CategoryDto>>> GetAllCategories([FromQuery] CategoryQueryParameters parameters)
        {
            var query = _context.Categories.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(parameters.Search))
            {
                var search = parameters.Search.ToLower();

                query = query.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    c.Description.ToLower().Contains(search));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(parameters.Status))
            {
                // Try to parse as enum string first
                if (Enum.TryParse<CategoryStatus>(parameters.Status, out var status))
                {
                    query = query.Where(c => c.Status == status);
                }
                // If that fails, try to parse as integer
                else if (int.TryParse(parameters.Status, out var statusInt))
                {
                    query = query.Where(c => (int)c.Status == statusInt);
                }
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

            var categories = await query
                .Include(c => c.Assets)
                .OrderBy(c => c.Name)
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            // Convert to DTOs with string status values
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Status = c.Status.ToString(),
                AssetsCount = c.Assets.Count,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();

            return Ok(new PaginatedResponse<CategoryDto>
            {
                Data = categoryDtos,
                Total = total,
                Page = parameters.Page,
                PageSize = parameters.PageSize,
                TotalPages = totalPages
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound(new ApiResponse<CategoryDto>
                {
                    Success = false,
                    Message = "Category not found"
                });
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Status = category.Status.ToString(),
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return Ok(new ApiResponse<CategoryDto>
            {
                Data = categoryDto,
                Message = "Category retrieved successfully"
            });
        }

        [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryDto request)
    {
        // Check if category name already exists
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower());

        if (existingCategory != null)
        {
            return BadRequest(new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = "A category with this name already exists"
            });
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Status = CategoryStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Status = category.Status.ToString(),
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new ApiResponse<CategoryDto>
        {
            Data = categoryDto,
            Message = "Category created successfully"
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto request)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            return NotFound(new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = "Category not found"
            });
        }

        // Check if name is being changed and if it conflicts with existing category
        if (!string.IsNullOrEmpty(request.Name) && request.Name.ToLower() != category.Name.ToLower())
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.Id != id);

            if (existingCategory != null)
            {
                return BadRequest(new ApiResponse<CategoryDto>
                {
                    Success = false,
                    Message = "A category with this name already exists"
                });
            }
        }

        // Update properties
        if (!string.IsNullOrEmpty(request.Name))
            category.Name = request.Name;

        if (!string.IsNullOrEmpty(request.Description))
            category.Description = request.Description;

        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Status = category.Status.ToString(),
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return Ok(new ApiResponse<CategoryDto>
        {
            Data = categoryDto,
            Message = "Category updated successfully"
        });
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategoryStatus(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = "Category not found"
            });
        }

        category.Status ^= CategoryStatus.INACTIVE ^ CategoryStatus.ACTIVE;

        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Status = category.Status.ToString(),
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return Ok(new ApiResponse<CategoryDto>
        {
            Data = categoryDto,
            Message = "Category status updated successfully"
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.Assets)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Category not found"
            });
        }

        // Check if category has associated assets
        if (category.Assets.Any())
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Cannot delete category that has associated assets"
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>
        {
            Message = "Category deleted successfully"
        });
    }
}
