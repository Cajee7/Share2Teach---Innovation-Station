using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Data Transfer Object (DTO) for representing user information.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    /// <value>
    /// The first name of the user.
    /// </value>
    [Required(ErrorMessage = "First name is required.")]
    public string? FirstName { get; set; } // Nullable property for the user's first name

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    /// <value>
    /// The last name of the user.
    /// </value>
    [Required(ErrorMessage = "Last name is required.")]
    public string? LastName { get; set; } // Nullable property for the user's last name

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    /// <value>
    /// The email address of the user.
    /// </value>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; } // Nullable property for the user's email

    /// <summary>
    /// Gets or sets the user's role.
    /// </summary>
    /// <value>
    /// The role of the user (e.g., "User", "Admin", "Teacher").
    /// </value>
    [Required(ErrorMessage = "Role is required.")]
    public string? Role { get; set; } // Nullable property for the user's role

    /// <summary>
    /// Gets or sets the list of subjects for the user.
    /// </summary>
    /// <value>
    /// A list of subjects associated with the user, typically required for teachers.
    /// </value>
    public List<string>? Subjects { get; set; } = new List<string>(); // Nullable property for subjects (optional)
}
