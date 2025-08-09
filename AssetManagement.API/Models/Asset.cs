using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AssetManagement.API.Enums;

namespace AssetManagement.API.Models;

public class Asset
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public Guid CategoryId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string SerialNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime PurchaseDate { get; set; } // UTC

    [Required]
    public AssetStatus Status { get; set; } = AssetStatus.Available;
    
    public Guid? AssignedToId { get; set; }
    
    public DateTime? AssignedAt { get; set; }
    
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; } = null!;
    
    [ForeignKey("AssignedToId")]
    public virtual User? AssignedTo { get; set; }
    
    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();
}
