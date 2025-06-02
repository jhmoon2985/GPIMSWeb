using System.ComponentModel.DataAnnotations;

namespace GPIMSWeb.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(100)]
        public string Department { get; set; }
        
        [Required]
        public UserLevel Level { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool? IsActive { get; set; }  // nullable bool로 변경
    }

    public enum UserLevel
    {
        Operate = 1,
        Maintenance = 2,
        Admin = 3
    }

    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        public string Language { get; set; } = "en";
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public UserLevel Level { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(100)]
        public string Department { get; set; }
        
        [Required]
        public UserLevel Level { get; set; }
    }
}