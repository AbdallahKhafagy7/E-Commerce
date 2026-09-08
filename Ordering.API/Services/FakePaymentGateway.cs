namespace Ordering.API.Services;

public sealed class FakePaymentGateway(ILogger<FakePaymentGateway> logger) : IPaymentGateway
{
    public Task<PaymentResult> ChargeAsync(decimal amount, string customerEmail, CancellationToken cancellationToken)
    {
        var transactionId = $"fake-pay-{Guid.NewGuid():N}";
        logger.LogInformation(
            "Fake payment succeeded for {CustomerEmail}. Amount: {Amount}. Transaction: {TransactionId}",
            customerEmail,
            amount,
            transactionId);

        return Task.FromResult(new PaymentResult(true, transactionId, null));
    }

    public Task RefundAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken)
    {
        logger.LogError(
            "Refund triggered for transaction {TransactionId}. Amount: {Amount}. Reason: {Reason}",
            transactionId,
            amount,
            reason);

        return Task.CompletedTask;
    }
}
