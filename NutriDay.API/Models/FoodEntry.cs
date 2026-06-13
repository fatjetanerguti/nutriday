namespace NutriDay.API.Models
{
    public class FoodEntry
    {
        public int Id { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public string RawInput { get; set; } = string.Empty;
        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = string.Empty;
    }
}