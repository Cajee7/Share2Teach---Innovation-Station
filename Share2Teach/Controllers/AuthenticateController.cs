using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using BCrypt.Net; // Used to hash passwords
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticateController : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _usersCollection;
        private readonly IConfiguration _configuration;

        public AuthenticateController(IMongoDatabase database, IConfiguration configuration)
        {
            _usersCollection = database.GetCollection<BsonDocument>("Users"); // Connecting to specific collection
            _configuration = configuration;
        }

        // POST: api/authenticate/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.Password != model.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            // Normalize role to lowercase for comparison
            var normalizedRole = model.Role.ToLower();
            if (normalizedRole != "teacher" && normalizedRole != "user")
                return BadRequest(new { message = "Only Teacher or User roles are allowed for registration." });

            var existingUser = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync();
            if (existingUser != null)
                return BadRequest(new { message = "User already exists" });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Teacher role validation: checks for institutional email and subjects
            if (normalizedRole == "teacher")
            {
                if (!model.Email.EndsWith(".edu") && !model.Email.EndsWith(".gov"))
                    return BadRequest(new { message = "Invalid email! Cannot register as a teacher." });

                if (model.Subjects == null || model.Subjects.Count == 0)
                    return BadRequest(new { message = "Teachers must provide at least one subject." });
            }

            var newUser = new BsonDocument
            {
                { "FirstName", model.FirstName }, // Updated to FirstName
                { "LastName", model.LastName }, // Updated to LastName
                { "Email", model.Email },
                { "Password", hashedPassword },
                { "Role", normalizedRole } // Store role in lowercase
            };

            if (normalizedRole == "teacher")
            {
                newUser["Subjects"] = new BsonArray(model.Subjects); // Add subjects for teachers
            }

            await _usersCollection.InsertOneAsync(newUser);

            return Ok(new { message = normalizedRole == "teacher" ? "Successfully registered as a teacher" : "User registered successfully" });
        }

        // POST: api/authenticate/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user["Password"].AsString))
                return Unauthorized(new { message = "Invalid login attempt" });

            // Generate the JWT token
            var token = GenerateJwtToken(user);

            // Set the cookie options (optional, if you want to store it in cookies)
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"]))
            };
            Response.Cookies.Append("jwt", token, cookieOptions);

            // Return the token along with the success message
            return Ok(new 
            { 
                message = "Logged in successfully", 
                token = token // Include the JWT token in the response
            });
        }

        // POST: api/authenticate/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync();
            if (user == null)
                return BadRequest(new { message = "User with this email does not exist" });

            var resetToken = Guid.NewGuid().ToString();
            var resetTokenExpiration = DateTime.UtcNow.AddMinutes(30);

            var update = Builders<BsonDocument>.Update
                .Set("ResetToken", resetToken)
                .Set("ResetTokenExpiration", resetTokenExpiration);
            await _usersCollection.UpdateOneAsync(new BsonDocument("Email", model.Email), update);

            SendResetEmail(model.Email, resetToken);

            return Ok(new { message = "Password reset token sent to email" });
        }

        // POST: api/authenticate/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (model.NewPassword != model.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var user = await _usersCollection.Find(new BsonDocument("ResetToken", model.Token)).FirstOrDefaultAsync();
            if (user == null)
                return BadRequest(new { message = "Invalid or expired reset token" });

            var resetTokenExpiration = user["ResetTokenExpiration"].ToUniversalTime();
            if (DateTime.UtcNow > resetTokenExpiration)
                return BadRequest(new { message = "Reset token has expired" });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            var update = Builders<BsonDocument>.Update
                .Set("Password", hashedPassword)
                .Unset("ResetToken")
                .Unset("ResetTokenExpiration");

            await _usersCollection.UpdateOneAsync(new BsonDocument("ResetToken", model.Token), update);

            return Ok(new { message = "Password has been reset successfully" });
        }

        // Method to send the reset token via email
        private void SendResetEmail(string email, string resetToken)
        {
            var resetLink = $"https://yourapp.com/reset-password?token={resetToken}";
            var emailContent = $"Click the link to reset your password: {resetLink}";

            // Email sending logic to be added
        }

        // Helper method to generate JWT token
        private string GenerateJwtToken(BsonDocument user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user["Email"].AsString),
                new Claim(ClaimTypes.Name, $"{user["FirstName"]} {user["LastName"]}"), // Updated to FirstName and LastName
                new Claim(ClaimTypes.Role, user["Role"].AsString.ToLower()), // Ensure role is used as lowercase
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // PUT: api/authenticate/upgrade
        [HttpPut("upgrade")]
        [Authorize(Roles = "admin")] // Authorization check for Admin
        public async Task<IActionResult> UpgradeUser([FromQuery] string email, [FromBody] string newRole)
        {
            // Normalize new role to lowercase for storage
            var normalizedNewRole = newRole.ToLower();

            // Find the user by email
            var user = await _usersCollection.Find(new BsonDocument("Email", email)).FirstOrDefaultAsync();
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Get the current role of the user
            var currentRole = user["Role"].AsString;

            // Define validation logic based on current role
            if (currentRole == "user")
            {
                if (normalizedNewRole != "admin")
                    return BadRequest(new { message = "A user can only be upgraded to an Admin role." });
            }
            else if (currentRole == "teacher")
            {
                if (normalizedNewRole != "moderator")
                    return BadRequest(new { message = "A teacher can only be upgraded to a Moderator role." });
            }
            else
            {
                return BadRequest(new { message = "Only users or teachers can be upgraded." });
            }

            // Perform the role update if validation passes
            var update = Builders<BsonDocument>.Update.Set("Role", normalizedNewRole);
            await _usersCollection.UpdateOneAsync(new BsonDocument("Email", email), update);

            return Ok(new { message = $"User upgraded to {normalizedNewRole} successfully." });
        }

        // GET: api/authenticate/current-user
        [HttpGet("current-user")]
        [Authorize] // Requires a valid token
        public IActionResult GetCurrentUser()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value; // Retrieve user's name
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value.ToLower(); // Retrieve user's role as lowercase

            return Ok(new 
            { 
                Name = userName,
                Role = userRole
            });
        }
    }
}
