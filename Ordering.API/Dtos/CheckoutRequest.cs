using System.ComponentModel.DataAnnotations;

namespace Ordering.API.Dtos;

public sealed record CheckoutRequest(
    [property: Required, MaxLength(100)] string UserId,
    [property: Required, EmailAddress, MaxLength(256)] string CustomerEmail,
    [property: Required, MaxLength(500)] string ShippingAddress,
    [property: MinLength(1)] IReadOnlyList<CheckoutItemRequest> Items,
    bool SimulateDatabaseFailure = false);
