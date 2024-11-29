namespace PaymentGateway.Api.Helpers
{
    public static class PaymentCardHelper
    {
        /// <summary>
        /// Masks a given card number, leaving only the last 4 digits intact.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <returns>The last 4 digits.</returns>
        public static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length <= 4)
                return cardNumber;

            return cardNumber.Substring(cardNumber.Length - 4, 4);
        }
    }
}
