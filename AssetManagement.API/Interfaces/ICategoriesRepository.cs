using AssetManagement.API.DTOs;
using AssetManagement.API.Models;

namespace AssetManagement.API.Interfaces;

public interface ICategoriesRepository
{
    Task<PaginatedResponse<CategoryDto>> GetCategories(CategoryQueryParameters parameters);
    Task<PaginatedResponse<CategoryDto>> GetAllCategories(CategoryQueryParameters parameters);
    Task<ApiResponse<CategoryDto>> GetCategory(Guid id);
    Task<ApiResponse<CategoryDto>> CreateCategory(CreateCategoryDto request);
    Task<ApiResponse<CategoryDto>> UpdateCategory(Guid id, UpdateCategoryDto request);
    Task<ApiResponse<CategoryDto>> UpdateCategoryStatus(Guid id);
    Task<ApiResponse<object>> DeleteCategory(Guid id);
}