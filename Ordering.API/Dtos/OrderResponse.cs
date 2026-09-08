namespace Ordering.API.Dtos;

public sealed record OrderResponse(
    Guid OrderId,
    string UserId,
    string CustomerEmail,
    string ShippingAddress,
    decimal TotalAmount,
    string Status,
    string? PaymentTransactionId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemResponse> Items);
