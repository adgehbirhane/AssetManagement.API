using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AutoMapper;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UsersController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers([FromQuery] QueryParameters parameters)
    {
        var query = _context.Users.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(parameters.Search))
        {
            query = query.Where(u => 
                u.FirstName.Contains(parameters.Search) || 
                u.LastName.Contains(parameters.Search) ||
                u.Email.Contains(parameters.Search));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        var userDtos = _mapper.Map<List<UserDto>>(users);

        return Ok(new PaginatedResponse<UserDto>
        {
            Data = userDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found"
            });
        }

        var userDto = _mapper.Map<UserDto>(user);

        return Ok(new ApiResponse<UserDto>
        {
            Data = userDto,
            Message = "User retrieved successfully"
        });
    }
}
