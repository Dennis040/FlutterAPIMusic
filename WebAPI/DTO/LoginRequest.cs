namespace WebAPI.DTO
{
    public class LoginRequest
    {
        public string Username { get; set; } // có thể là username hoặc email
        public string Password { get; set; }
    }

}
