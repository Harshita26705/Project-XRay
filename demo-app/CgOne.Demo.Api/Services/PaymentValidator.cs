using CgOne.Demo.Api.Models;

namespace CgOne.Demo.Api.Services;

public interface IPaymentValidator
{
    PaymentResult Validate(PaymentRequest request);
}

/// <summary>
/// The component the X-Ray demo scenario modifies.
/// </summary>
public class PaymentValidator : IPaymentValidator
{
    private const decimal MaximumAmount = 10000m;

    public PaymentResult Validate(PaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            return new PaymentResult { Accepted = false, Reason = "Amount must be positive." };
        }

        if (request.Amount > MaximumAmount)
        {
            return new PaymentResult { Accepted = false, Reason = "Amount exceeds the limit." };
        }

        if (string.IsNullOrWhiteSpace(request.CardNumber) || request.CardNumber.Length < 12)
        {
            return new PaymentResult { Accepted = false, Reason = "Card number is invalid." };
        }

        return new PaymentResult { Accepted = true, Reason = "OK" };
    }
}
