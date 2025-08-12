using AssetManagement.API.DTOs;
using AssetManagement.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.API.Interfaces;

public interface IAssetsRepository
{
    Task<AssetListResponse> GetAssets(AssetQueryParameters parameters);
    Task<ApiResponse<AssetDto>> GetAsset(Guid id);
    Task<ApiResponse<AssetDto>> CreateAsset(CreateAssetRequest request, IFormFile? image);
    Task<ApiResponse<AssetDto>> UpdateAsset(Guid id, UpdateAssetRequest request, IFormFile? image);
    Task<ApiResponse<AssetDto>> UpdateAssetStatus(Guid id, UpdateAssetStatusRequest request);
    Task<ApiResponse<object>> DeleteAsset(Guid id);
    Task<string> SaveAssetImageAsync(IFormFile imageFile);
    void DeleteAssetImage(string imageUrl);
    IActionResult GetImage(string fileName);
}