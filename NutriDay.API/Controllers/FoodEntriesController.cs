using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriDay.API.Data;
using NutriDay.API.Models;
using NutriDay.API.Services;
using System.Security.Claims;

namespace NutriDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FoodEntriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AiService _aiService;

        public FoodEntriesController(AppDbContext context, AiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        private string GetUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        // GET: api/foodentries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FoodEntry>>> GetAll()
        {
            var userId = GetUserId();
            return await _context.FoodEntries
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        // GET: api/foodentries/today
        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<FoodEntry>>> GetToday()
        {
            var userId = GetUserId();
            var today = DateTime.UtcNow.Date;
            return await _context.FoodEntries
                .Where(f => f.UserId == userId && f.CreatedAt.Date == today)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        // POST: api/foodentries
        [HttpPost]
        public async Task<ActionResult<FoodEntry>> Create([FromBody] CreateFoodRequest request)
        {
            var userId = GetUserId();
            var nutrition = await _aiService.AnalyzeFoodAsync(request.RawInput);

            var entry = new FoodEntry
            {
                RawInput = request.RawInput,
                FoodName = nutrition.FoodName,
                Calories = nutrition.Calories,
                Protein = nutrition.Protein,
                Carbs = nutrition.Carbs,
                Fat = nutrition.Fat,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            _context.FoodEntries.Add(entry);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = entry.Id }, entry);
        }

        // DELETE: api/foodentries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var entry = await _context.FoodEntries
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (entry == null) return NotFound();

            _context.FoodEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class CreateFoodRequest
    {
        public string RawInput { get; set; } = "";
    }
}