namespace WebAPI.Models
{
    public class ConfirmPaymentRequest
    {
        public int UserId { get; set; }
        public double Amount { get; set; }
        public int DurationDays { get; set; }
    }

}
