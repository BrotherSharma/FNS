using System;
using System.ComponentModel.DataAnnotations;

namespace FNS.Models
{
    public class WorkoutLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Username { get; set; }
        
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; }
        
        public int DurationMinutes { get; set; }
        
        public double CaloriesBurned { get; set; }
        
        public DateTime LogDate { get; set; } = DateTime.UtcNow;
    }
}
