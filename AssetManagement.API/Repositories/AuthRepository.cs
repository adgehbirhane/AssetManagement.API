using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Enums;
using AssetManagement.API.Interfaces;
using AssetManagement.API.Models;
using AssetManagement.API.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetManagement.API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;

    public AuthRepository(
        ApplicationDbContext context,
        IJwtService jwtService,
        IPasswordService passwordService,
        IMapper mapper)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var token = _jwtService.GenerateToken(user);
        var userDto = _mapper.Map<UserDto>(user);

        return new ApiResponse<AuthResponse>
        {
            Data = new AuthResponse
            {
                User = userDto,
                Token = token
            },
            Message = "Login successful"
        };
    }

    public async Task<ApiResponse<AuthResponse>> Register(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Email already exists"
            };
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.User,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        var userDto = _mapper.Map<UserDto>(user);

        return new ApiResponse<AuthResponse>
        {
            Data = new AuthResponse
            {
                User = userDto,
                Token = token
            },
            Message = "Registration successful"
        };
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUser(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Invalid or expired token"
            };
        }

        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (currentUser == null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found"
            };
        }

        var userDto = _mapper.Map<UserDto>(currentUser);

        return new ApiResponse<UserDto>
        {
            Data = userDto,
            Message = "User retrieved successfully"
        };
    }
}