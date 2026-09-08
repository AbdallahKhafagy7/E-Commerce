using Ordering.API.Domain;
using Ordering.API.Dtos;

namespace Ordering.API.Services;

public static class OrderMapping
{
    public static OrderResponse ToResponse(this Order order)
    {
        return new OrderResponse(
            order.Id,
            order.UserId,
            order.CustomerEmail,
            order.ShippingAddress,
            order.TotalAmount,
            order.Status.ToString(),
            order.PaymentTransactionId,
            order.CreatedAt,
            order.Items
                .OrderBy(item => item.ProductNameSnapshot)
                .Select(item => new OrderItemResponse(
                    item.ProductId,
                    item.ProductNameSnapshot,
                    item.UnitPriceSnapshot,
                    item.Quantity,
                    item.LineTotal))
                .ToList());
    }
}
