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

            if (model.Role != "Teacher" && model.Role != "User" && model.Role != "teacher" && model.Role != "user")
                return BadRequest(new { message = "Only Teacher or User roles are allowed for registration." });

            var existingUser = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync();
            if (existingUser != null)
                return BadRequest(new { message = "User already exists" });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Teacher role validation: checks for institutional email and subjects
            if (model.Role == "Teacher")
            {
                if (!model.Email.EndsWith(".edu") && !model.Email.EndsWith(".gov"))
                    return BadRequest(new { message = "Invalid email! Cannot register as a teacher." });

                if (model.Subjects == null || model.Subjects.Count == 0)
                    return BadRequest(new { message = "Teachers must provide at least one subject." });
            }

            var newUser = new BsonDocument
            {
                { "FName", model.FName },
                { "LName", model.LName },
                { "Email", model.Email },
                { "Password", hashedPassword },
                { "Role", model.Role }
            };

            if (model.Role == "Teacher")
            {
                newUser["Subjects"] = new BsonArray(model.Subjects); // Add subjects for teachers
            }

            await _usersCollection.InsertOneAsync(newUser);

            return Ok(new { message = model.Role == "Teacher" ? "Successfully registered as a teacher" : "User registered successfully" });
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

            var token = GenerateJwtToken(user);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"]))
            };
            Response.Cookies.Append("jwt", token, cookieOptions);

            return Ok(new { message = "Logged in successfully" });
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
                new Claim(ClaimTypes.Name, $"{user["FName"]} {user["LName"]}"),
                new Claim(ClaimTypes.Role, user["Role"].AsString),
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
    }
}
