using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver; //database 
using MongoDB.Bson;
using BCrypt.Net; // Used to hash passwords
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Net.Mail; // For email functionality
using System.Net;

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticateController : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _usersCollection;
        private readonly IConfiguration _configuration; //configuration settings for jwt and smtp

        public AuthenticateController(IMongoDatabase database, IConfiguration configuration)
        {
            _usersCollection = database.GetCollection<BsonDocument>("Users"); // Connecting to specific collection
            _configuration = configuration; //configuration settings for jwt and smtp
        }

        // POST: api/authenticate/register
        [HttpPost("register")] //account creation endpoint 
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {
            if (!ModelState.IsValid) //checks is model is valid
                return BadRequest(ModelState);

            if (model.Password != model.ConfirmPassword) //checks if the passwords match 
                return BadRequest(new { message = "Passwords do not match" });

            var normalizedRole = model.Role.ToLower(); //makes sure only users and teachers can signup 
            if (normalizedRole != "teacher" && normalizedRole != "user")
                return BadRequest(new { message = "Only Teacher or User roles are allowed for registration." });

            var existingUser = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync(); 
            if (existingUser != null)
                return BadRequest(new { message = "User already exists" }); //makes sure only one user per email

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password); //hashes password for secure saving in DB

            if (normalizedRole == "teacher") 
            {
                if (!model.Email.EndsWith(".edu") && !model.Email.EndsWith(".gov") && !model.Email.EndsWith(".org"))//can only use the teacher role with a valid teachers email
                    return BadRequest(new { message = "Invalid email! Cannot register as a teacher." });

                if (model.Subjects == null || model.Subjects.Count == 0) //teachers have to teach provide atleast one subject that they teach
                    return BadRequest(new { message = "Teachers must provide at least one subject." });
            }

            var newUser = new BsonDocument //creates the new user
            {
                { "FirstName", model.FirstName },
                { "LastName", model.LastName },
                { "Email", model.Email },
                { "Password", hashedPassword },
                { "Role", normalizedRole }
            };

            if (normalizedRole == "teacher") //only saves subjects if it is a teacher
            {
                newUser["Subjects"] = new BsonArray(model.Subjects);
            }

            await _usersCollection.InsertOneAsync(newUser);

            return Ok(new { message = normalizedRole == "teacher" ? "Successfully registered as a teacher" : "User registered successfully" });
        }

        // POST: api/authenticate/login
        [HttpPost("login")] //sign in endpoint
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync(); //looks for email in DB

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user["Password"].AsString)) //compares the password to the hashed password 
                return Unauthorized(new { message = "Invalid login attempt" });

            var token = GenerateJwtToken(user); //calls the method that generates a jwt token

            var cookieOptions = new CookieOptions //sets the jwt token in a cookie for security
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"]))
            };
            Response.Cookies.Append("jwt", token, cookieOptions);

            return Ok(new 
            { 
                message = "Logged in successfully", 
                token = token 
            }); //returns a message aswell as a token that gets saved in authorization
        }

        // POST: api/authenticate/forgot-password
        [HttpPost("forgot-password")] //part of password reset
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _usersCollection.Find(new BsonDocument("Email", model.Email)).FirstOrDefaultAsync(); //checks to see if user with this email exists
            if (user == null)
                return BadRequest(new { message = "User with this email does not exist" });

            var resetToken = Guid.NewGuid().ToString(); //creates a reset token
            var resetTokenExpiration = DateTime.UtcNow.AddMinutes(30);

            var update = Builders<BsonDocument>.Update
                .Set("ResetToken", resetToken)
                .Set("ResetTokenExpiration", resetTokenExpiration);
            await _usersCollection.UpdateOneAsync(new BsonDocument("Email", model.Email), update);

            SendResetEmail(model.Email, resetToken); //email method to send a reset email to user

            return Ok(new { message = "Password reset token sent to email" });
        }

        // POST: api/authenticate/reset-password
        [HttpPost("reset-password")] //second part of the reset password endpoint
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (model.NewPassword != model.ConfirmPassword) //compares password
                return BadRequest(new { message = "Passwords do not match" });

            var user = await _usersCollection.Find(new BsonDocument("ResetToken", model.Token)).FirstOrDefaultAsync(); //checks token validity
            if (user == null)
                return BadRequest(new { message = "Invalid or expired reset token" });

            var resetTokenExpiration = user["ResetTokenExpiration"].ToUniversalTime(); //ensures token can only be used for a certain amount of time 
            if (DateTime.UtcNow > resetTokenExpiration)
                return BadRequest(new { message = "Reset token has expired" });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword); //hash new password 
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
            var smtpSettings = _configuration.GetSection("SmtpSettings"); //getting smtp connection settings 
            var emailContent = $"Your password reset token is: {resetToken}. Please use this token to reset your password."; //token send in email 

            var mailMessage = new MailMessage //message that user will receive to reset password
            {
                From = new MailAddress(smtpSettings["SenderEmail"], smtpSettings["SenderName"]),
                Subject = "Password Reset Request",
                Body = emailContent,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            using (var smtpClient = new SmtpClient(smtpSettings["Server"], int.Parse(smtpSettings["Port"]))) //connects to group email to send from 
            {
                smtpClient.Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]);
                smtpClient.EnableSsl = bool.Parse(smtpSettings["EnableSsl"]);
                smtpClient.Send(mailMessage);
            }
        }

        // Helper method to generate JWT token
        private string GenerateJwtToken(BsonDocument user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])); //uses key stored in json file
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] //creates token using the following info
            {
                new Claim(JwtRegisteredClaimNames.Sub, user["Email"].AsString),
                new Claim(ClaimTypes.Name, $"{user["FirstName"]} {user["LastName"]}"),
                new Claim(ClaimTypes.Role, user["Role"].AsString.ToLower()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
                signingCredentials: credentials
            ); //information that token has

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // PUT: api/authenticate/upgrade
        [HttpPut("upgrade")] //endpoint allowing admin to make a teacher a moderator and a user an admin
        [Authorize(Roles = "admin")] //checks that user who is logged in is a admin
        public async Task<IActionResult> UpgradeUser([FromQuery] string email, [FromBody] string newRole) 
        {
            var normalizedNewRole = newRole.ToLower(); //makes sure no matter how admin enters role it is stored in small letters

            var user = await _usersCollection.Find(new BsonDocument("Email", email)).FirstOrDefaultAsync();
            if (user == null)
                return NotFound(new { message = "User not found" }); //checks if user exists

            var currentRole = user["Role"].AsString; //checks the users current role

            //ensures that user can only be upgraded based on what their current role is 
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

            var update = Builders<BsonDocument>.Update.Set("Role", normalizedNewRole); //update role
            await _usersCollection.UpdateOneAsync(new BsonDocument("Email", email), update);

            return Ok(new { message = $"User upgraded to {normalizedNewRole} successfully." });
        }

        // GET: api/authenticate/current-user
        [HttpGet("current-user")] //endpoint to get user details
        [Authorize] //reads token put into authorization
        public IActionResult GetCurrentUser()
        {
            //retrieves the users information from the token
            var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var firstName = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                Email = email,
                Name = firstName,
                Role = role
            }); //returns the user information
        }
    }
}
