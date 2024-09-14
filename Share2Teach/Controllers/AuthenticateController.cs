using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using BCrypt.Net;
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
            _usersCollection = database.GetCollection<BsonDocument>("Users");
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
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Find the user by email
            var user = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync();

            if (user == null)
                return Unauthorized(new { message = "Invalid login attempt" });

            // Check if the password matches
            var storedPassword = user["Password"].AsString;
            if (!BCrypt.Net.BCrypt.Verify(model.Password, storedPassword))
                return Unauthorized(new { message = "Invalid login attempt" });

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new { token });
        }

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
