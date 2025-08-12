using AssetManagement.API.DTOs;
using AssetManagement.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoriesRepository _repository;

    public CategoriesController(ICategoriesRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CategoryDto>>> GetCategories([FromQuery] CategoryQueryParameters parameters)
    {
        try
        {
            var result = await _repository.GetCategories(parameters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("all")]
    [Authorize]
    public async Task<ActionResult<PaginatedResponse<CategoryDto>>> GetAllCategories([FromQuery] CategoryQueryParameters parameters)
    {
        try
        {
            var result = await _repository.GetAllCategories(parameters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(Guid id)
    {
        try
        {
            var result = await _repository.GetCategory(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = $"Failed to retrieve category: {ex.Message}"
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryDto request)
    {
        try
        {
            var result = await _repository.CreateCategory(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetCategory), new { id = result.Data?.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = $"Failed to create category: {ex.Message}"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto request)
    {
        try
        {
            var result = await _repository.UpdateCategory(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = $"Failed to update category: {ex.Message}"
            });
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategoryStatus(Guid id)
    {
        try
        {
            var result = await _repository.UpdateCategoryStatus(id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<CategoryDto>
            {
                Success = false,
                Message = $"Failed to update category status: {ex.Message}"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(Guid id)
    {
        try
        {
            var result = await _repository.DeleteCategory(id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"Failed to delete category: {ex.Message}"
            });
        }
    }
}