using AssetManagement.API.Enums;
using AssetManagement.API.Models;
using System.ComponentModel.DataAnnotations;

namespace AssetManagement.API.DTOs;

public class CreateAssetRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string SerialNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime PurchaseDate { get; set; }
    
    public IFormFile? Image { get; set; }
}

public class UpdateAssetRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }
    
    public Guid? CategoryId { get; set; }
    
    [MaxLength(255)]
    public string? SerialNumber { get; set; }
    
    public DateTime? PurchaseDate { get; set; }
    
    public IFormFile? Image { get; set; }
}

public class UpdateAssetStatusRequest
{    
    [Required]
    //[EnumDataType(typeof(AssetStatus))]
    public string Status { get; set; } = string.Empty;
}

public class AssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public CategoryDto Category { get; set; } = null!;
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedToId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDto? AssignedTo { get; set; }
}

public class AssetListResponse : PaginatedResponse<AssetDto>
{
}

public class AssetQueryParameters : QueryParameters
{
    public string? Category { get; set; }
    public string? Status { get; set; }
}


