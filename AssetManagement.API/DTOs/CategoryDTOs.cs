using System.ComponentModel.DataAnnotations;
using AssetManagement.API.Enums;

namespace AssetManagement.API.DTOs
{
    public class CreateCategoryDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

    }

    public class UpdateCategoryDto
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

    }

    public class CategoryDto
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(CategoryStatus))]
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CategoryQueryParameters : QueryParameters
    {
        public string? Status { get; set; }
    }

}
