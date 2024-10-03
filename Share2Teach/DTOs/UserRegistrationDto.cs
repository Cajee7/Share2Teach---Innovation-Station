using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Data transfer object for user registration.
/// </summary>
public class UserRegistrationDto
{
    /// <summary>
    /// Gets or sets the first name of the user.
    /// This field is required and must be between 2 and 15 characters.
    /// </summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(15, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 15 characters.")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// This field is required and must be between 2 and 20 characters.
    /// </summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 20 characters.")]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the email of the user.
    /// This field is required and must be a valid email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the password of the user.
    /// This field is required and must be at least 8 characters long.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the confirmation password for the user.
    /// Must match the password field.
    /// </summary>
    [Required(ErrorMessage = "Confirmation password is required.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    /// <summary>
    /// Gets or sets the role of the user.
    /// This field is required. Defaults to "User".
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    public string? Role { get; set; } = "User"; // Default role is "User"

    /// <summary>
    /// Gets or sets the subjects the user teaches.
    /// Only required for users with a teacher role.
    /// </summary>
    public List<string>? Subjects { get; set; } = new List<string>(); // Optional for teachers
}
