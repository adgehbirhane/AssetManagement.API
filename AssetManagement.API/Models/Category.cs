using System.ComponentModel.DataAnnotations;
using AssetManagement.API.Enums;

namespace AssetManagement.API.Models
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public CategoryStatus Status { get; set; } = CategoryStatus.ACTIVE;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
