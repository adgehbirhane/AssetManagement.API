using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AssetManagement.API.Enums;

namespace AssetManagement.API.Models;

public class AssetRequest
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid AssetId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public AssetRequestStatus Status { get; set; } = AssetRequestStatus.Pending;
    
    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; } 
    
    public Guid? ProcessedById { get; set; }
    
    // Navigation properties
    [ForeignKey("AssetId")]
    public virtual Asset Asset { get; set; } = null!;
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("ProcessedById")]
    public virtual User? ProcessedBy { get; set; }
}
