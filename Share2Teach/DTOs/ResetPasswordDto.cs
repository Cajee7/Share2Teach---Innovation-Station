using System.ComponentModel.DataAnnotations;

/// <summary>
/// Data Transfer Object (DTO) for handling password reset requests.
/// </summary>
public class ResetPasswordDto
{
    /// <summary>
    /// Gets or sets the reset token.
    /// </summary>
    /// <value>
    /// The token sent to the user for resetting their password.
    /// </value>
    [Required(ErrorMessage = "Token is required.")]
    public string? Token { get; set; } // Nullable property for the reset token

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    /// <value>
    /// The new password that the user wants to set.
    /// </value>
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; } // Nullable property for the new password

    /// <summary>
    /// Gets or sets the confirmation password.
    /// </summary>
    /// <value>
    /// The password entered again for confirmation, must match the new password.
    /// </value>
    [Required(ErrorMessage = "Confirmation password is required.")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; } // Nullable property for the confirmation password
}
