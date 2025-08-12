using AssetManagement.API.DTOs;
using AssetManagement.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetsRepository _repository;

    public AssetsController(IAssetsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<AssetListResponse>> GetAssets([FromQuery] AssetQueryParameters parameters)
    {
        try
        {
            var result = await _repository.GetAssets(parameters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> GetAsset(Guid id)
    {
        try
        {
            var result = await _repository.GetAsset(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AssetDto>
            {
                Success = false,
                Message = $"Failed to retrieve asset: {ex.Message}"
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> CreateAsset([FromForm] CreateAssetRequest request)
    {
        try
        {
            var result = await _repository.CreateAsset(request, request.Image);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetAsset), new { id = result.Data?.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AssetDto>
            {
                Success = false,
                Message = $"Failed to create asset: {ex.Message}"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> UpdateAsset(Guid id, [FromForm] UpdateAssetRequest request)
    {
        try
        {
            var result = await _repository.UpdateAsset(id, request, request.Image);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AssetDto>
            {
                Success = false,
                Message = $"Failed to update asset: {ex.Message}"
            });
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> UpdateAssetStatus(Guid id, [FromBody] UpdateAssetStatusRequest request)
    {
        try
        {
            var result = await _repository.UpdateAssetStatus(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AssetDto>
            {
                Success = false,
                Message = $"Failed to update asset status: {ex.Message}"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsset(Guid id)
    {
        try
        {
            var result = await _repository.DeleteAsset(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"Failed to delete asset: {ex.Message}"
            });
        }
    }

    [HttpGet("images/{fileName}")]
    [AllowAnonymous]
    public IActionResult GetImage(string fileName)
    {
        try
        {
            return _repository.GetImage(fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"Failed to retrieve image: {ex.Message}"
            });
        }
    }
}