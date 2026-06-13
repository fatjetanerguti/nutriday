using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriDay.API.Data;

namespace NutriDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext _context;

        public AdminController(UserManager<IdentityUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var entryCount = await _context.FoodEntries
                    .CountAsync(f => f.UserId == user.Id);
                var lastEntry = await _context.FoodEntries
                    .Where(f => f.UserId == user.Id)
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefaultAsync();

                result.Add(new
                {
                    user.Id,
                    user.Email,
                    Roles = roles,
                    EntryCount = entryCount,
                    LastActive = lastEntry?.CreatedAt
                });
            }

            return Ok(result);
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Fshi të gjitha entries e userit
            var entries = _context.FoodEntries.Where(f => f.UserId == id);
            _context.FoodEntries.RemoveRange(entries);
            await _context.SaveChangesAsync();

            // Fshi userin
            await _userManager.DeleteAsync(user);
            return Ok(new { message = "User deleted successfully." });
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalEntries = await _context.FoodEntries.CountAsync();
            var todayEntries = await _context.FoodEntries
                .CountAsync(f => f.CreatedAt.Date == DateTime.UtcNow.Date);

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalEntries = totalEntries,
                TodayEntries = todayEntries
            });
        }
    }
}