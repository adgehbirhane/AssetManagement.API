using AssetManagement.API.DTOs;
using AssetManagement.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthRepository _repository;

    public AuthController(IAuthRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _repository.Login(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = $"Login failed: {ex.Message}"
            });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _repository.Register(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = $"Registration failed: {ex.Message}"
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        try
        {
            var result = await _repository.GetCurrentUser(User);

            if (!result.Success)
            {
                if (result.Message == "Invalid or expired token")
                    return Unauthorized(result);

                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = $"Failed to retrieve user information: {ex.Message}"
            });
        }
    }
}