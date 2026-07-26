using System;
using System.ComponentModel.DataAnnotations;

namespace FNS.Models
{
    public class FitnessProfile
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Username { get; set; } // Link to the user system
        
        public int Age { get; set; }
        public double Height { get; set; } // in cm
        public double Weight { get; set; } // in kg
        public string Gender { get; set; }
        public string FitnessLevel { get; set; } // Beginner, Intermediate, Advanced
        public string Goal { get; set; } // Lose Weight, Gain Weight, Build Muscle, Stay Fit
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
