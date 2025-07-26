namespace WebAPI.DTO
{
    public class EmailVerificationRequest
    {
        public int UserId { get; set; }
        public string VerificationCode { get; set; }
    }
}
