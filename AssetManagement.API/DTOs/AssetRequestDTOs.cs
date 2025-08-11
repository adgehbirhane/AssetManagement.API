using System.ComponentModel.DataAnnotations;
using AssetManagement.API.Enums;

namespace AssetManagement.API.DTOs;

public class CreateAssetRequestDto
{
    [Required]
    public Guid AssetId { get; set; }
}

public class UpdateAssetRequestDto
{
    [Required]
    public AssetRequestStatus Status { get; set; }
}

public class AssetRequestDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedById { get; set; }
    public AssetDto Asset { get; set; } = null!;
    public UserDto User { get; set; } = null!;
    public UserDto? ProcessedBy { get; set; }
}

public class AssetRequestListResponse
{
    public List<AssetRequestDto> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AssetRequestQueryParameters
{
    public string? Search { get; set; }
    public DateTime? RequestedFrom { get; set; }
    public DateTime? RequestedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
}


