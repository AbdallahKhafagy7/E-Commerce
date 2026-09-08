namespace Ordering.API.Services;

public sealed record PaymentResult(bool Succeeded, string? TransactionId, string? FailureReason);
