using System.ComponentModel.DataAnnotations; // Include the namespace for validation attributes

public class UserRegistrationDto
{
    [Required(ErrorMessage = "First name is required.")] // Ensures the first name is provided
    [StringLength(15, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 15 characters.")] // Sets min and max length of the first name
    public string FName { get; set; }         

    [Required(ErrorMessage = "Last name is required.")] // Ensures the last name is provided
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 20 characters.")] // Sets min and max length of the last name
    public string LName { get; set; }          

    [Required(ErrorMessage = "Email is required.")] // Ensures the email is provided
    [EmailAddress(ErrorMessage = "Invalid email format.")] // Validates the email format
    public string Email { get; set; }          

    [Required(ErrorMessage = "Password is required.")] // Ensures the password is provided
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")] // Sets a minimum length for the password
    [DataType(DataType.Password)] // Specifies that this field is a password
    public string Password { get; set; }       

    [Required(ErrorMessage = "Confirmation password is required.")] // Ensures the confirmation password is provided
    [Compare("Password", ErrorMessage = "Passwords do not match.")] // Validates that the confirmation password matches the password
    [DataType(DataType.Password)] // Specifies that this field is a password
    public string ConfirmPassword { get; set; } 

   [Required(ErrorMessage = "At least one subject is required.")]
   [MinLength(1, ErrorMessage = "You must provide at least one subject.")]
    public List<string> Subjects { get; set; } = new List<string>(); 
}
