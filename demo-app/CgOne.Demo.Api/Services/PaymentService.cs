using CgOne.Demo.Api.Models;
using CgOne.Demo.Api.Repositories;

namespace CgOne.Demo.Api.Services;

public interface IPaymentService
{
    Task<PaymentResult> ProcessAsync(PaymentRequest request);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentValidator _validator;
    private readonly IPaymentRepository _repository;

    public PaymentService(IPaymentValidator validator, IPaymentRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {
        var validation = _validator.Validate(request);
        if (!validation.Accepted)
        {
            return validation;
        }

        var transaction = new PaymentTransaction
        {
            OrderId = request.OrderId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = "CAPTURED"
        };

        var id = await _repository.SaveAsync(transaction);

        return new PaymentResult { Accepted = true, Reason = "Captured", TransactionId = id };
    }
}
