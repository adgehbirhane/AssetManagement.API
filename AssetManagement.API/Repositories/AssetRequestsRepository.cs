using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Models;
using AssetManagement.API.Enums;
using AssetManagement.API.Interfaces;
using AutoMapper;

namespace AssetManagement.API.Repositories;

public class AssetRequestsRepository : IAssetRequestsRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AssetRequestsRepository(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AssetRequestListResponse> GetAssetRequests(AssetRequestQueryParameters parameters, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        var query = _context.AssetRequests
            .Include(ar => ar.Asset)
               .ThenInclude(a => a.Category)
            .Include(ar => ar.User)
            .Include(ar => ar.ProcessedBy)
            .AsQueryable();

        if (userRole != "Admin")
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                throw new UnauthorizedAccessException();
            }
            query = query.Where(ar => ar.UserId == userGuid);
        }

        query = ApplyFilters(query, parameters);

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / parameters.PageSize);

        var assetRequests = await query
            .OrderByDescending(ar => ar.RequestedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        var assetRequestDtos = _mapper.Map<List<AssetRequestDto>>(assetRequests);

        return new AssetRequestListResponse
        {
            Data = assetRequestDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        };
    }

    public async Task<AssetRequestListResponse> GetMyAssetRequests(AssetRequestQueryParameters parameters, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            throw new UnauthorizedAccessException();
        }

        var query = _context.AssetRequests
            .Include(ar => ar.Asset)
               .ThenInclude(a => a.Category)
            .Include(ar => ar.User)
            .Include(ar => ar.ProcessedBy)
            .Where(ar => ar.UserId == userGuid);

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

        var assetRequestDtos = _mapper.Map<List<AssetRequestDto>>(assetRequests);

        return new AssetRequestListResponse
        {
            Data = assetRequestDtos,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = totalPages
        };
    }

    public async Task<ApiResponse<AssetRequestDto>> CreateAssetRequest(CreateAssetRequestDto request, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            throw new UnauthorizedAccessException();
        }

        var asset = await _context.Assets.FindAsync(request.AssetId);
        if (asset == null)
        {
            return new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "Asset not found"
            };
        }

        if (asset.Status != AssetStatus.Available)
        {
            return new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "Asset is not available"
            };
        }

        var existingRequest = await _context.AssetRequests
            .FirstOrDefaultAsync(ar => ar.AssetId == request.AssetId &&
                                     ar.UserId == userGuid &&
                                     ar.Status == AssetRequestStatus.Pending);

        if (existingRequest != null)
        {
            return new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "You already have a pending request for this asset"
            };
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

        await _context.Entry(assetRequest)
            .Reference(ar => ar.Asset)
            .LoadAsync();
        await _context.Entry(assetRequest)
            .Reference(ar => ar.User)
            .LoadAsync();

        var assetRequestDto = _mapper.Map<AssetRequestDto>(assetRequest);

        return new ApiResponse<AssetRequestDto>
        {
            Data = assetRequestDto,
            Message = "Asset request created successfully"
        };
    }

    public async Task<ApiResponse<AssetRequestDto>> GetAssetRequest(Guid id, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        var assetRequest = await _context.AssetRequests
            .Include(ar => ar.Asset)
               .ThenInclude(a => a.Category)
            .Include(ar => ar.User)
            .Include(ar => ar.ProcessedBy)
            .FirstOrDefaultAsync(ar => ar.Id == id);

        if (assetRequest == null)
        {
            return new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "Asset request not found"
            };
        }

        if (userRole != "Admin" && assetRequest.UserId.ToString() != userId)
        {
            throw new UnauthorizedAccessException();
        }

        var assetRequestDto = _mapper.Map<AssetRequestDto>(assetRequest);

        return new ApiResponse<AssetRequestDto>
        {
            Data = assetRequestDto,
            Message = "Asset request retrieved successfully"
        };
    }

    public async Task<ApiResponse<AssetRequestDto>> UpdateAssetRequest(Guid id, UpdateAssetRequestDto request, ClaimsPrincipal user)
    {
        var adminUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId) || !Guid.TryParse(adminUserId, out var adminUserGuid))
        {
            throw new UnauthorizedAccessException();
        }

        var assetRequest = await _context.AssetRequests
            .Include(ar => ar.Asset)
               .ThenInclude(a => a.Category)
            .Include(ar => ar.User)
            .Include(ar => ar.ProcessedBy)
            .FirstOrDefaultAsync(ar => ar.Id == id);

        if (assetRequest == null)
        {
            return new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "Asset request not found"
            };
        }

        if (assetRequest.Status != AssetRequestStatus.Pending)
        {
            return new ApiResponse<AssetRequestDto>
            {
                Success = false,
                Message = "Asset request has already been processed"
            };
        }

        assetRequest.Status = request.Status;
        assetRequest.ProcessedAt = DateTime.UtcNow;
        assetRequest.ProcessedById = adminUserGuid;

        if (request.Status == AssetRequestStatus.Approved)
        {
            var asset = await _context.Assets.FindAsync(assetRequest.AssetId);
            if (asset != null)
            {
                if (asset.Status == AssetStatus.Assigned)
                {
                    return new ApiResponse<AssetRequestDto>
                    {
                        Success = false,
                        Message = "This asset is already assigned to another user."
                    };
                }
                asset.Status = AssetStatus.Assigned;
                asset.AssignedToId = assetRequest.UserId;
                asset.AssignedAt = DateTime.UtcNow;
                asset.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        var assetRequestDto = _mapper.Map<AssetRequestDto>(assetRequest);

        return new ApiResponse<AssetRequestDto>
        {
            Data = assetRequestDto,
            Message = $"Asset request {request.Status} successfully"
        };
    }

    private IQueryable<AssetRequest> ApplyFilters(IQueryable<AssetRequest> query, AssetRequestQueryParameters parameters)
    {
        if (!string.IsNullOrEmpty(parameters.Search))
        {
            query = query.Where(ar =>
                ar.Asset.Name.ToLower().Contains(parameters.Search.ToLower()) ||
                ar.Asset.SerialNumber.ToLower().Contains(parameters.Search.ToLower()) ||
                ar.User.FirstName.ToLower().Contains(parameters.Search.ToLower()) ||
                ar.User.LastName.ToLower().Contains(parameters.Search.ToLower()) ||
                ar.User.Email.ToLower().Contains(parameters.Search.ToLower()));
        }

        if (!string.IsNullOrEmpty(parameters.Status))
        {
            if (Enum.TryParse<AssetRequestStatus>(parameters.Status, out var status))
            {
                query = query.Where(ar => ar.Status == status);
            }
        }

        if (parameters.RequestedFrom.HasValue)
        {
            query = query.Where(ar => ar.RequestedAt >= parameters.RequestedFrom.Value);
        }
        if (parameters.RequestedTo.HasValue)
        {
            query = query.Where(ar => ar.RequestedAt <= parameters.RequestedTo.Value);
        }

        return query;
    }
}