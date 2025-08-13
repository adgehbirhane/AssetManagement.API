using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AssetManagement.API.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    [DefaultValue("admin@gmail.com")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(100, ErrorMessage = "Password must not exceed 100 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special character.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    [DefaultValue("exampleuser@gmail.com")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(100, ErrorMessage = "Password must not exceed 100 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special character.")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name must not exceed 100 characters.")]
    [RegularExpression(@"^[A-Za-z\-'\s]+$", ErrorMessage = "First name can only contain letters, spaces, apostrophes, and hyphens.")]
    [DefaultValue("Abebe")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name must not exceed 100 characters.")]
    [RegularExpression(@"^[A-Za-z\-'\s]+$", ErrorMessage = "Last name can only contain letters, spaces, apostrophes, and hyphens.")]
    [DefaultValue("Bikila")]
    public string LastName { get; set; } = string.Empty;
}

public class AuthResponse
{
    public UserDto User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ValidateTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [EmailAddress]
    public string? Email { get; set; }
}


