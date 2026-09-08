namespace Ordering.API.Domain;

public enum OrderStatus
{
    Pending = 1,
    PaymentSucceeded = 2,
    Completed = 3,
    PaymentFailed = 4,
    Refunded = 5
}
