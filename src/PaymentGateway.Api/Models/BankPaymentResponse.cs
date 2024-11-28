namespace PaymentGateway.Api.Models
{
    public class BankPaymentResponse
    {
        public bool Authorized { get; set; }
        public string AuthorizationCode { get; set; }
    }
}
