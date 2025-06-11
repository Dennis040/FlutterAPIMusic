using System.ComponentModel.DataAnnotations;
namespace WebAPI.DTO
{
    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password phải có ít nhất 6 ký tự.")]
        public string Password { get; set; }

        public string? Phone { get; set; }

        public string? Role { get; set; } = "user"; // mặc định là user
    }

}
