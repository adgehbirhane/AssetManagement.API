using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Models;
using AssetManagement.API.Enums;
using AutoMapper;

namespace AssetManagement.API.Controllers;

[ApiController]
[Route("api/asset-requests")]
[Authorize]
public class AssetRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AssetRequestsController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<AssetRequestListResponse>> GetAssetRequests([FromQuery] AssetRequestQueryParameters parameters)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var query = _context.AssetRequests
            .Include(ar => ar.Asset)
               .ThenInclude(a => a.Category)
            .Include(ar => ar.User)
            .Include(ar => ar.ProcessedBy)
            .AsQueryable();

        // If user is not admin, only show their own requests
        if (userRole != "Admin")
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }
            query = query.Where(ar => ar.UserId == userGuid);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(parameters.Search))
        {
            query = query.Where(ar =>
                ar.Asset.Name.ToLower().Contains(parameters.Search.ToLower()) ||
                ar.Asset.SerialNumber.ToLower().Contains(parameters.Search.ToLower()) ||
                ar.User.FirstName.ToLower().Contains(parameters.Search.ToLower()) ||
                ar.User.LastName.ToLower().Contains(parameters.Search.ToLower()) ||
                ar.User.Email.ToLower().Contains(parameters.Search.ToLower()));
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(parameters.Status))
        {
            if (Enum.TryParse<AssetRequestStatus>(parameters.Status, out var status))
            {
                query = query.Where(ar => ar.Status == status);
            }
        }

        // Apply requested date range filter
        if (parameters.RequestedFrom.HasValue)
        {
            query = query.Where(ar => ar.RequestedAt >= parameters.RequestedFrom.Value);
        }
        if (parameters.RequestedTo.HasValue)
        {
            query = query.Where(ar => ar.RequestedAt <= parameters.RequestedTo.Value);
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var assetRequests = await query
            .OrderByDescending(ar => ar.RequestedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        // Convert to DTOs with string status values
        var assetRequestDtos = assetRequests.Select(ar => new AssetRequestDto
        {
            Id = ar.Id,
            AssetId = ar.AssetId,
            UserId = ar.UserId,
            Status = ar.Status.ToString(),
            RequestedAt = ar.RequestedAt,
            ProcessedAt = ar.ProcessedAt,
            ProcessedById = ar.ProcessedById,
            Asset = new AssetDto
            {
                Id = ar.Asset.Id,
                Name = ar.Asset.Name,
                CategoryId = ar.Asset.CategoryId,
                Category = new CategoryDto
                {
                    Id = ar.Asset.Category.Id,
                    Name = ar.Asset.Category.Name,
                    Description = ar.Asset.Category.Description,
                    Status = ar.Asset.Category.Status.ToString(),
                    CreatedAt = ar.Asset.Category.CreatedAt,
                    UpdatedAt = ar.Asset.Category.UpdatedAt
                },
                SerialNumber = ar.Asset.SerialNumber,
                PurchaseDate = ar.Asset.PurchaseDate,
                Status = ar.Asset.Status.ToString(),
                AssignedToId = ar.Asset.AssignedToId,
                AssignedAt = ar.Asset.AssignedAt,
                ImageUrl = ar.Asset.ImageUrl,
                CreatedAt = ar.Asset.CreatedAt,
                UpdatedAt = ar.Asset.UpdatedAt
            },
            User = new UserDto
            {
                Id = ar.User.Id,
                Email = ar.User.Email,
                FirstName = ar.User.FirstName,
                LastName = ar.User.LastName,
                Role = ar.User.Role.ToString(),
                ProfileImageUrl = ar.User.ProfileImageUrl,
                CreatedAt = ar.User.CreatedAt,
                UpdatedAt = ar.User.UpdatedAt
            },
            ProcessedBy = ar.ProcessedBy != null ? new UserDto
            {
                Id = ar.ProcessedBy.Id,
                Email = ar.ProcessedBy.Email,
                FirstName = ar.ProcessedBy.FirstName,
                LastName = ar.ProcessedBy.LastName,
                Role = ar.ProcessedBy.Role.ToString(),
                CreatedAt = ar.ProcessedBy.CreatedAt,
                UpdatedAt = ar.ProcessedBy.UpdatedAt
            } : null
        }).ToList();

        return Ok(new AssetRequestListResponse
        {
            Data = assetRequestDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        });
    }


    [HttpGet("self")]
    public async Task<ActionResult<AssetRequestListResponse>> GetMyAssetRequests([FromQuery] AssetRequestQueryParameters parameters)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var query = _context.AssetRequests
            .Include(ar => ar.Asset)
               .ThenInclude(a => a.Category)
            .Include(ar => ar.User)
            .Include(ar => ar.ProcessedBy)
            .AsQueryable();

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }
            query = query.Where(ar => ar.UserId == userGuid);

        // Apply status filter
        if (!string.IsNullOrEmpty(parameters.Status))
        {
            if (Enum.TryParse<AssetRequestStatus>(parameters.Status, out var status))
            {
                query = query.Where(ar => ar.Status == status);
            }
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var assetRequests = await query
            .OrderByDescending(ar => ar.RequestedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        // Convert to DTOs with string status values
        var assetRequestDtos = assetRequests.Select(ar => new AssetRequestDto
        {
            Id = ar.Id,
            AssetId = ar.AssetId,
            UserId = ar.UserId,
            Status = ar.Status.ToString(),
            RequestedAt = ar.RequestedAt,
            ProcessedAt = ar.ProcessedAt,
            ProcessedById = ar.ProcessedById,
            Asset = new AssetDto
            {
                Id = ar.Asset.Id,
                Name = ar.Asset.Name,
                CategoryId = ar.Asset.CategoryId,
                Category = new CategoryDto
                {
                    Id = ar.Asset.Category.Id,
                    Name = ar.Asset.Category.Name,
                    Description = ar.Asset.Category.Description,
                    Status = ar.Asset.Category.Status.ToString(),
                    CreatedAt = ar.Asset.Category.CreatedAt,
                    UpdatedAt = ar.Asset.Category.UpdatedAt
                },
                SerialNumber = ar.Asset.SerialNumber,
                PurchaseDate = ar.Asset.PurchaseDate,
                Status = ar.Asset.Status.ToString(),
                AssignedToId = ar.Asset.AssignedToId,
                AssignedAt = ar.Asset.AssignedAt,
                ImageUrl = ar.Asset.ImageUrl,
                CreatedAt = ar.Asset.CreatedAt,
                UpdatedAt = ar.Asset.UpdatedAt
            },
            User = new UserDto
            {
                Id = ar.User.Id,
                Email = ar.User.Email,
                FirstName = ar.User.FirstName,
                LastName = ar.User.LastName,
                Role = ar.User.Role.ToString(),
                ProfileImageUrl = ar.User.ProfileImageUrl,
                CreatedAt = ar.User.CreatedAt,
                UpdatedAt = ar.User.UpdatedAt
            },
            ProcessedBy = ar.ProcessedBy != null ? new UserDto
            {
                Id = ar.ProcessedBy.Id,
                Email = ar.ProcessedBy.Email,
                FirstName = ar.ProcessedBy.FirstName,
                LastName = ar.ProcessedBy.LastName,
                Role = ar.ProcessedBy.Role.ToString(),
                CreatedAt = ar.ProcessedBy.CreatedAt,
                UpdatedAt = ar.ProcessedBy.UpdatedAt
            } : null
        }).ToList();

        return Ok(new AssetRequestListResponse
        {
            Data = assetRequestDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        });
    }


    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssetRequestDto>>> CreateAssetRequest([FromBody] CreateAssetRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            // Check if asset exists and is available
            var asset = await _context.Assets.FindAsync(request.AssetId);
            if (asset == null)
            {
                return NotFound(new ApiResponse<AssetRequestDto>
                {
                    Success = false,
                    Message = "Asset not found"
                });
            }

            if (asset.Status != AssetStatus.Available)
            {
                return BadRequest(new ApiResponse<AssetRequestDto>
                {
                    Success = false,
                    Message = "Asset is not available"
                });
            }

            // Check if user already has a pending request for this asset
            var existingRequest = await _context.AssetRequests
                .FirstOrDefaultAsync(ar => ar.AssetId == request.AssetId && 
                                         ar.UserId == userGuid && 
                                         ar.Status == AssetRequestStatus.Pending);

            if (existingRequest != null)
            {
                return BadRequest(new ApiResponse<AssetRequestDto>
                {
                    Success = false,
                    Message = "You already have a pending request for this asset"
                });
            }

            var assetRequest = new AssetRequest
            {
                Id = Guid.NewGuid(),
                AssetId = request.AssetId,
                UserId = userGuid,
                Status = AssetRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.AssetRequests.Add(assetRequest);
            await _context.SaveChangesAsync();

            // Load related data for response
            await _context.Entry(assetRequest)
                .Reference(ar => ar.Asset)
                .LoadAsync();
            await _context.Entry(assetRequest)
                .Reference(ar => ar.User)
                .LoadAsync();

            var assetRequestDto = _mapper.Map<AssetRequestDto>(assetRequest);

            return CreatedAtAction(nameof(GetAssetRequest), new { id = assetRequest.Id }, new ApiResponse<AssetRequestDto>
            {
                Data = assetRequestDto,
                Message = "Asset request created successfully"
            });
        }
        catch (Exception ex)
        {
            // Log the detailed error
            //Console.WriteLine($"Error creating asset request: {ex.Message}");
            //Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
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
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var assetRequest = await _context.AssetRequests
            .Include(ar => ar.Asset)
               .ThenInclude(a => a.Category)
            .Include(ar => ar.User)
            .Include(ar => ar.ProcessedBy)
            .FirstOrDefaultAsync(ar => ar.User.Id == id);

        if (assetRequest == null)
        {
            return NotFound(new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "Asset request not found"
            });
        }

        // Check if user has permission to view this request
        if (userRole != "Admin" && assetRequest.UserId.ToString() != userId)
        {
            return Forbid();
        }

        var assetRequestDto = _mapper.Map<AssetRequestDto>(assetRequest);

        return Ok(new ApiResponse<AssetRequestDto>
        {
            Data = assetRequestDto,
            Message = "Asset request retrieved successfully"
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AssetRequestDto>>> UpdateAssetRequest(Guid id, [FromBody] UpdateAssetRequestDto request)
    {
        var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId) || !Guid.TryParse(adminUserId, out var adminUserGuid))
        {
            return Unauthorized();
        }

        var assetRequest = await _context.AssetRequests
            .Include(ar => ar.Asset)
               .ThenInclude(a => a.Category)
            .Include(ar => ar.User)
            .Include(ar => ar.ProcessedBy)
            .FirstOrDefaultAsync(ar => ar.Id == id);

        if (assetRequest == null)
        {
            return NotFound(new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "Asset request not found"
            });
        }

        if (assetRequest.Status != AssetRequestStatus.Pending)
        {
            return BadRequest(new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "Asset request has already been processed"
            });
        }

        assetRequest.Status = request.Status;
        assetRequest.ProcessedAt = DateTime.UtcNow;
        assetRequest.ProcessedById = adminUserGuid;

        // If approved, update asset status and assignment
        if (request.Status == AssetRequestStatus.Approved)
        {
            var asset = await _context.Assets.FindAsync(assetRequest.AssetId);
            if (asset != null)
            {
                asset.Status = AssetStatus.Assigned;
                asset.AssignedToId = assetRequest.UserId;
                asset.AssignedAt = DateTime.UtcNow;
                asset.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        var assetRequestDto = _mapper.Map<AssetRequestDto>(assetRequest);

        return Ok(new ApiResponse<AssetRequestDto>
        {
            Data = assetRequestDto,
            Message = $"Asset request {request.Status} successfully"
        });
    }
}
