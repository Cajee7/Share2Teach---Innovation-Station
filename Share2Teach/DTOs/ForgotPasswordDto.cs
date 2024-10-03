using System.ComponentModel.DataAnnotations;

/// <summary>
/// Data Transfer Object (DTO) for handling forgot password requests.
/// </summary>
public class ForgotPasswordDto
{
    /// <summary>
    /// Gets or sets the email address associated with the user account.
    /// </summary>
    /// <value>
    /// The email address used for sending password reset instructions.
    /// </value>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; } // Nullable property for the email address
}
