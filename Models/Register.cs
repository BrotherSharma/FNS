// FNS.Models/Register.cs
using System.ComponentModel.DataAnnotations;

namespace FNS.Models
{
    public class Register
    {
        public string  c_username { get; set; }
        public string  c_email { get; set; }
        public string c_password{get; set;}
        public string c_role { get; set; }
        public string c_firstname { get; set; }
        public string c_lastname { get; set; }
        
    }
}