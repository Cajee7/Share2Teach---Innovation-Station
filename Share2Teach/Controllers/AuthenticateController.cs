using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticateController : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _usersCollection;

        public AuthenticateController(IMongoDatabase database)
        {
            // Get the Users collection
            _usersCollection = database.GetCollection<BsonDocument>("Users");
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

            // Combine first and last names for the username
            string userName = $"{model.FName} {model.LName}";

            // Create a new user document
            var newUser = new BsonDocument
            {
                { "FName", model.FName },
                { "LName", model.LName },
                { "Email", model.Email },
                { "Password", model.Password }, // Consider hashing the password before storing
                { "Role", "User" },
                { "Subject", model.Subject }
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
            if (storedPassword != model.Password) // Implement password hashing and comparison
                return Unauthorized(new { message = "Invalid login attempt" });

            // Optionally, generate and return a JWT token here if required
            return Ok(new { message = "Login successful" });
        }
    }
}
