using AssetManagement.API.Data;
using AssetManagement.API.DTOs;
using AssetManagement.API.Models;
using AssetManagement.API.Enums;
using System.Security.Claims;

namespace AssetManagement.API.Interfaces;

public interface IAssetRequestsRepository
{
    Task<AssetRequestListResponse> GetAssetRequests(AssetRequestQueryParameters parameters, ClaimsPrincipal user);
    Task<AssetRequestListResponse> GetMyAssetRequests(AssetRequestQueryParameters parameters, ClaimsPrincipal user);
    Task<ApiResponse<AssetRequestDto>> CreateAssetRequest(CreateAssetRequestDto request, ClaimsPrincipal user);
    Task<ApiResponse<AssetRequestDto>> GetAssetRequest(Guid id, ClaimsPrincipal user);
    Task<ApiResponse<AssetRequestDto>> UpdateAssetRequest(Guid id, UpdateAssetRequestDto request, ClaimsPrincipal user);
}