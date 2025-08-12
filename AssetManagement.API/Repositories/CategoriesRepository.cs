using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Enums;
using AssetManagement.API.Interfaces;
using AssetManagement.API.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.API.Repositories;

public class CategoriesRepository : ICategoriesRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CategoriesRepository(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponse<CategoryDto>> GetCategories(CategoryQueryParameters parameters)
    {
        var query = _context.Categories
            .Where(c => c.Status == CategoryStatus.ACTIVE)
            .AsQueryable();

        query = ApplySearchFilter(query, parameters.Search);

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var categories = await query
            .Include(c => c.Assets)
            .OrderBy(c => c.Name)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        var categoryDtos = _mapper.Map<List<CategoryDto>>(categories);

        return new PaginatedResponse<CategoryDto>
        {
            Data = categoryDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        };
    }

    public async Task<PaginatedResponse<CategoryDto>> GetAllCategories(CategoryQueryParameters parameters)
    {
        var query = _context.Categories.AsQueryable();

        query = ApplySearchFilter(query, parameters.Search);
        query = ApplyStatusFilter(query, parameters.Status);

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var categories = await query
            .Include(c => c.Assets)
            .OrderBy(c => c.Name)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        var categoryDtos = _mapper.Map<List<CategoryDto>>(categories);

        return new PaginatedResponse<CategoryDto>
        {
            Data = categoryDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        };
    }

    public async Task<ApiResponse<CategoryDto>> GetCategory(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            return new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = "Category not found"
            };
        }

        var categoryDto = _mapper.Map<CategoryDto>(category);
        return new ApiResponse<CategoryDto>
        {
            Data = categoryDto,
            Message = "Category retrieved successfully"
        };
    }

    public async Task<ApiResponse<CategoryDto>> CreateCategory(CreateCategoryDto request)
    {
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower());

        if (existingCategory != null)
        {
            return new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = "A category with this name already exists"
            };
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

        var categoryDto = _mapper.Map<CategoryDto>(category);
        return new ApiResponse<CategoryDto>
        {
            Data = categoryDto,
            Message = "Category created successfully"
        };
    }

    public async Task<ApiResponse<CategoryDto>> UpdateCategory(Guid id, UpdateCategoryDto request)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            return new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = "Category not found"
            };
        }

        if (!string.IsNullOrEmpty(request.Name) && request.Name.ToLower() != category.Name.ToLower())
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.Id != id);

            if (existingCategory != null)
            {
                return new ApiResponse<CategoryDto>
                {
                    Success = false,
                    Message = "A category with this name already exists"
                };
            }
        }

        if (!string.IsNullOrEmpty(request.Name))
            category.Name = request.Name;

        if (!string.IsNullOrEmpty(request.Description))
            category.Description = request.Description;

        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var categoryDto = _mapper.Map<CategoryDto>(category);
        return new ApiResponse<CategoryDto>
        {
            Data = categoryDto,
            Message = "Category updated successfully"
        };
    }

    public async Task<ApiResponse<CategoryDto>> UpdateCategoryStatus(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = "Category not found"
            };
        }

        category.Status = category.Status == CategoryStatus.ACTIVE
            ? CategoryStatus.INACTIVE
            : CategoryStatus.ACTIVE;

        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var categoryDto = _mapper.Map<CategoryDto>(category);
        return new ApiResponse<CategoryDto>
        {
            Data = categoryDto,
            Message = "Category status updated successfully"
        };
    }

    public async Task<ApiResponse<object>> DeleteCategory(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.Assets)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Category not found"
            };
        }

        if (category.Assets.Any())
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "Cannot delete category that has associated assets"
            };
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return new ApiResponse<object>
        {
            Message = "Category deleted successfully"
        };
    }

    private IQueryable<Category> ApplySearchFilter(IQueryable<Category> query, string? searchTerm)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                c.Description.ToLower().Contains(search));
        }
        return query;
    }

    private IQueryable<Category> ApplyStatusFilter(IQueryable<Category> query, string? status)
    {
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<CategoryStatus>(status, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }
            else if (int.TryParse(status, out var statusInt))
            {
                query = query.Where(c => (int)c.Status == statusInt);
            }
        }
        return query;
    }
}