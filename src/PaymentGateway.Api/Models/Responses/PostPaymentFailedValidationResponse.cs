namespace PaymentGateway.Api.Models.Responses
{
    public class PostPaymentFailedValidationResponse
    {
        public PaymentStatus Status => PaymentStatus.Rejected;
        public FriendlyValidationError[] Errors { get; set; }
    }
}
