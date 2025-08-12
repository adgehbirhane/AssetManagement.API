using AssetManagement.API.DTOs;
using AssetManagement.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUsersRepository _repository;

    public UsersController(IUsersRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers([FromQuery] QueryParameters parameters)
    {
        try
        {
            var result = await _repository.GetUsers(parameters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var result = await _repository.GetUser(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = $"Failed to retrieve user: {ex.Message}"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UpdateProfileRequest request)
    {
        try
        {
            var result = await _repository.UpdateUser(id, request, User);

            if (!result.Success)
            {
                if (result.Message == "Forbidden") return Forbid();
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Internal server error occurred while updating profile"
            });
        }
    }

    [HttpPost("{id}/profile-image")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> UploadProfileImage(Guid id, IFormFile imageFile)
    {
        try
        {
            var result = await _repository.UploadProfileImage(id, imageFile, User);

            if (!result.Success)
            {
                if (result.Message == "Forbidden") return Forbid();
                return BadRequest(result);
            }

            return Ok(result);
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
            return _repository.GetProfileImage(id);
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
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProfileImage(Guid id)
    {
        try
        {
            var result = await _repository.DeleteProfileImage(id, User);

            if (!result.Success)
            {
                if (result.Message == "Forbidden") return Forbid();
                return BadRequest(result);
            }

            return Ok(result);
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
}