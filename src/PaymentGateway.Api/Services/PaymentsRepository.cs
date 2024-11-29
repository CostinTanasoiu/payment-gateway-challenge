using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public interface IPaymentsRepository
{
    void Add(PaymentResponse payment);
    PaymentResponse Get(Guid id);
}

public class PaymentsRepository : IPaymentsRepository
{
    public List<PaymentResponse> Payments = new();

    public void Add(PaymentResponse payment)
    {
        Payments.Add(payment);
    }

    public PaymentResponse Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}