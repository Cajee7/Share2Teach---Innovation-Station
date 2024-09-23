using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class UserRegistrationDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(15, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 15 characters.")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 20 characters.")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirmation password is required.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }

    // Default role is "User"
    public string Role { get; set; } = "User";

    // Only required for teachers
    public List<string> Subjects { get; set; } = new List<string>();
}
