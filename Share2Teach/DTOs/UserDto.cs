public class UserDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public List<string> Subjects { get; set; } = new List<string>(); // Optional, based on your requirements
}
