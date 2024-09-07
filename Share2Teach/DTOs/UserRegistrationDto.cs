public class UserRegistrationDto
{
    public required string FName { get; set; }          // Required property for the user's first name
    public required string LName { get; set; }          // Required property for the user's last name
    public required string Email { get; set; }          // Required property for the user's email
    public required string Password { get; set; }       // Required property for the user's password
    public required string ConfirmPassword { get; set; } // Required property to confirm the password
    public required string Subject { get; set; }   // Required property for the subjects the educator teaches 
}
