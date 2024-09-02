using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DatabaseConnection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AuthenticateController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // POST: api/authenticate/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

         if (model.Password != model.ConfirmPassword)
            return BadRequest(new { message = "Passwords do not match" });

        // Combine first and last names for the username
        string userName = $"{model.FName} {model.LName}";

        var user = new IdentityUser
        {
            UserName = userName, // Set the username to the concatenated first and last name
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            return Ok(new { message = "User registered successfully" });
        }

        foreach (var error in result.Errors)
             ModelState.AddModelError(string.Empty, error.Description);

        return BadRequest(ModelState);
}


        // POST: api/authenticate/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Optionally, generate and return a JWT token here if required
                return Ok(new { message = "Login successful" });
            }

            return Unauthorized(new { message = "Invalid login attempt" });
        }
    }
}
