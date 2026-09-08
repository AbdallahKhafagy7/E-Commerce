using System.ComponentModel.DataAnnotations;

namespace Ordering.API.Dtos;

public sealed record CheckoutItemRequest(
    [property: Range(1, int.MaxValue)] int ProductId,
    [property: Range(1, 100)] int Quantity);
