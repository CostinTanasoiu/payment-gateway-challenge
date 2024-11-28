using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services
{
    public interface IAcquiringBankService
    {
        Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request);
    }

    public class AcquiringBankService : IAcquiringBankService
    {
        HttpClient _httpClient;
        public AcquiringBankService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("payments", request);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: '{responseContent}'");
            }

            var bankPaymentResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();
            return bankPaymentResponse;
        }
    }
}
