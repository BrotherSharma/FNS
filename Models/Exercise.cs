using System;
using System.ComponentModel.DataAnnotations;

namespace FNS.Models
{
    public class Exercise
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        
        public string ImageUrl { get; set; } // GIF/Image
        
        public string Difficulty { get; set; } // Beginner, Medium, Hard
        public string TargetMuscles { get; set; } // Comma separated, e.g., "Chest,Triceps"
        
        public string Type { get; set; } // Home, Gym
        public string Equipment { get; set; } // None, Dumbbells, etc.
        
        public double CaloriesBurnedPerMinute { get; set; }
    }
}
