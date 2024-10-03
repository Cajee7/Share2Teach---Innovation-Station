using System.ComponentModel.DataAnnotations;

/// <summary>
/// Data Transfer Object (DTO) for user login information.
/// </summary>
public class UserLoginDto
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    /// <value>
    /// A valid email address required for login.
    /// </value>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }  // Nullable property for the user's email

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    /// <value>
    /// A password that must be at least 8 characters long.
    /// </value>
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; } // Nullable property for the user's password
}
