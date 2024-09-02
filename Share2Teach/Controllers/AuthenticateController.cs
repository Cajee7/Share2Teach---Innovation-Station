using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

// Namespace should match your project structure
namespace DatabaseConnection.Controllers
{   
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticateController : ControllerBase
    {   
        private readonly UserManager<IdentityUser> _userManager;

        public AuthenticateController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // POST: api/authenticate/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {   
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new IdentityUser { UserName = model.Username, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
                return Ok(new { message = "User registered successfully" });

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return BadRequest(ModelState);
        }

        // Additional methods for login and logout can be added here
    }

    // DTO for User Registration
    public class UserRegistrationDto
    {
        public required string Username { get; set; } // Required property
        public required string Email { get; set; }    // Required property
        public required string Password { get; set; } // Required property
        public required string ConfirmPassword { get; set; } // Required property
    }
}
