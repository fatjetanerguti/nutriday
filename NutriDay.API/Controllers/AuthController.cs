using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NutriDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest request)
        {
            var user = new IdentityUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Registration successful!" });
        }

       [HttpPost("login")]
public async Task<IActionResult> Login([FromBody] AuthRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    if (user == null)
        return Unauthorized(new { message = "Invalid email or password." });

    var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
    if (!isValid)
        return Unauthorized(new { message = "Invalid email or password." });

    var roles = await _userManager.GetRolesAsync(user);
    var token = GenerateToken(user, roles);
    var isAdmin = roles.Contains("Admin");

    return Ok(new { token, email = user.Email, isAdmin });
}

private string GenerateToken(IdentityUser user, IList<string> roles)
{
    var jwtKey = _configuration["Jwt:Key"] ?? "nutriday-super-secret-key-2024";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email ?? "")
    };

    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddDays(30),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

        private string GenerateToken(IdentityUser user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "nutriday-super-secret-key-2024";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class AuthRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}