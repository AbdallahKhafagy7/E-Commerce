namespace Ordering.API.Services;

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(decimal amount, string customerEmail, CancellationToken cancellationToken);
    Task RefundAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken);
}
