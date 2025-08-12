using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AssetManagement.API.DTOs;
using AssetManagement.API.Interfaces;
using AutoMapper;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/asset-requests")]
[Authorize]
public class AssetRequestsController : ControllerBase
{
    private readonly IAssetRequestsRepository _repository;

    public AssetRequestsController(IAssetRequestsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<AssetRequestListResponse>> GetAssetRequests([FromQuery] AssetRequestQueryParameters parameters)
    {
        try
        {
            var result = await _repository.GetAssetRequests(parameters, User);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("self")]
    public async Task<ActionResult<AssetRequestListResponse>> GetMyAssetRequests([FromQuery] AssetRequestQueryParameters parameters)
    {
        try
        {
            var result = await _repository.GetMyAssetRequests(parameters, User);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssetRequestDto>>> CreateAssetRequest([FromBody] CreateAssetRequestDto request)
    {
        try
        {
            var result = await _repository.CreateAssetRequest(request, User);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetAssetRequest), new { id = result.Data?.Id }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = $"Failed to create asset request: {ex.Message}"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AssetRequestDto>>> GetAssetRequest(Guid id)
    {
        try
        {
            var result = await _repository.GetAssetRequest(id, User);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = $"Failed to retrieve asset request: {ex.Message}"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetRequestDto>>> UpdateAssetRequest(Guid id, [FromBody] UpdateAssetRequestDto request)
    {
        try
        {
            var result = await _repository.UpdateAssetRequest(id, request, User);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = $"Failed to update asset request: {ex.Message}"
            });
        }
    }
}