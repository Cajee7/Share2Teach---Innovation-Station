using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver; 
using MongoDB.Bson;
using BCrypt.Net; //used to hash passwords
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
            // Get the Users collection
            _usersCollection = database.GetCollection<BsonDocument>("Users"); //connecting to specififc collection
            _configuration = configuration;
        }

        // POST: api/authenticate/register
        [HttpPost("register")] //enpoint for user to create an account
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //checks if password and confirm password are the same
            if (model.Password != model.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" }); 

            // Check if the user already exists
            var existingUser = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync();
            if (existingUser != null)
                return BadRequest(new { message = "User already exists" });

            // Hash the password before storing
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Create a new user document
            var newUser = new BsonDocument
            {
                { "FName", model.FName },
                { "LName", model.LName },
                { "Email", model.Email },
                { "Password", hashedPassword }, // Store the hashed password
                { "Role", "User" },
                { "Subjects", new BsonArray(model.Subjects) }
            };

            // Insert the new user document into the Users collection
            await _usersCollection.InsertOneAsync(newUser);

            return Ok(new { message = "User registered successfully" });
        }

        // POST: api/authenticate/login
        [HttpPost("login")] //endpoint for existing users to login 
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Find the user by email
            var user = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync();

            //return error if user does not exist
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user["Password"].AsString))
                return Unauthorized(new { message = "Invalid login attempt" });

            // Generate JWT token
            var token = GenerateJwtToken(user);

            // Create a cookie with the token
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Makes it HTTP-only, not accessible via JavaScript
                Expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"]))
            };
            Response.Cookies.Append("jwt", token, cookieOptions);

            return Ok(new { message = "Logged in successfully" });
        }

        // POST: api/authenticate/forgot-password
        [HttpPost("forgot-password")] //endpoint for a user who forgot their password
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            //find the user by their email
            var user = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync();
            if (user == null)
                return BadRequest(new { message = "User with this email does not exist" });

            // Generate a reset token
            var resetToken = Guid.NewGuid().ToString();
            var resetTokenExpiration = DateTime.UtcNow.AddMinutes(30); // Set expiration for 30 minutes

            // Store the reset token and its expiration in the user document
            var update = Builders<BsonDocument>.Update
                .Set("ResetToken", resetToken)
                .Set("ResetTokenExpiration", resetTokenExpiration);
            await _usersCollection.UpdateOneAsync(new BsonDocument("Email", model.Email), update);

            // Send the reset token to the user's email
            SendResetEmail(model.Email, resetToken);

            return Ok(new { message = "Password reset token sent to email" });
        }

        // POST: api/authenticate/reset-password
        [HttpPost("reset-password")] //endpoint for user to change their password
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            //chechk if the password and confirm password match
            if (model.NewPassword != model.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var user = await _usersCollection.Find(new BsonDocument("ResetToken", model.Token)).FirstOrDefaultAsync();
            if (user == null)
                return BadRequest(new { message = "Invalid or expired reset token" });

            // Check if the reset token is expired
            var resetTokenExpiration = user["ResetTokenExpiration"].ToUniversalTime();
            if (DateTime.UtcNow > resetTokenExpiration)
                return BadRequest(new { message = "Reset token has expired" });

            // Hash the new password and update it
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            var update = Builders<BsonDocument>.Update
                .Set("Password", hashedPassword)
                .Unset("ResetToken") // Remove the used token
                .Unset("ResetTokenExpiration");

            await _usersCollection.UpdateOneAsync(new BsonDocument("ResetToken", model.Token), update);

            return Ok(new { message = "Password has been reset successfully" });
        }

        // Method to send the reset token via email
        private void SendResetEmail(string email, string resetToken)
        {
            var resetLink = $"https://yourapp.com/reset-password?token={resetToken}";
            
            
            var emailContent = $"Click the link to reset your password: {resetLink}";

            //still need to connect to connect to email service
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
