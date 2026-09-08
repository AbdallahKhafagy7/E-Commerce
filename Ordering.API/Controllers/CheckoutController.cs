using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Ordering.API.Data;
using Ordering.API.Domain;
using Ordering.API.Dtos;
using Ordering.API.Services;

namespace Ordering.API.Controllers;

[ApiController]
[Route("api/ordering/checkout")]
public sealed class CheckoutController(
    OrderingDbContext dbContext,
    ICatalogClient catalogClient,
    IPaymentGateway paymentGateway,
    ILogger<CheckoutController> logger) : ControllerBase
{
    private const int CheckoutProcessingStatusCode = 425;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    public async Task<IActionResult> Checkout(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new { message = "Idempotency-Key header is required." });
        }

        var requestHash = RequestHash.Create(request);
        var existingRecord = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.Key == idempotencyKey, cancellationToken);

        if (existingRecord is not null)
        {
            if (!string.Equals(existingRecord.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return Conflict(new { message = "This Idempotency-Key was already used with a different request body." });
            }

            if (existingRecord.StatusCode == CheckoutProcessingStatusCode)
            {
                return Conflict(new { message = "A checkout with this Idempotency-Key is already being processed." });
            }

            return new ContentResult
            {
                StatusCode = existingRecord.StatusCode,
                Content = existingRecord.ResponseJson,
                ContentType = "application/json"
            };
        }

        var idempotencyRecord = new IdempotencyRecord
        {
            Key = idempotencyKey,
            RequestHash = requestHash,
            StatusCode = CheckoutProcessingStatusCode,
            ResponseJson = JsonSerializer.Serialize(new { message = "Checkout is being processed." }, JsonOptions)
        };

        dbContext.IdempotencyRecords.Add(idempotencyRecord);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { message = "A checkout with this Idempotency-Key is already being processed." });
        }

        var snapshots = await catalogClient.GetProductsAsync(
            request.Items.Select(item => item.ProductId),
            cancellationToken);

        var missingProductIds = request.Items
            .Select(item => item.ProductId)
            .Where(productId => !snapshots.ContainsKey(productId))
            .Distinct()
            .ToArray();

        if (missingProductIds.Length > 0)
        {
            var missingProductsResponse = new
            {
                message = "Some products do not exist in the catalog.",
                missingProductIds
            };

            idempotencyRecord.StatusCode = StatusCodes.Status400BadRequest;
            idempotencyRecord.ResponseJson = JsonSerializer.Serialize(missingProductsResponse, JsonOptions);
            await dbContext.SaveChangesAsync(cancellationToken);

            return BadRequest(missingProductsResponse);
        }

        var orderItems = request.Items.Select(item =>
        {
            var product = snapshots[item.ProductId];
            var lineTotal = product.Price * item.Quantity;

            return new OrderItem
            {
                ProductId = item.ProductId,
                ProductNameSnapshot = product.Name,
                UnitPriceSnapshot = product.Price,
                Quantity = item.Quantity,
                LineTotal = lineTotal
            };
        }).ToList();

        var total = orderItems.Sum(item => item.LineTotal);
        var payment = await paymentGateway.ChargeAsync(total, request.CustomerEmail, cancellationToken);

        if (!payment.Succeeded || payment.TransactionId is null)
        {
            var paymentFailedResponse = new
            {
                message = "Payment failed.",
                reason = payment.FailureReason
            };

            idempotencyRecord.StatusCode = StatusCodes.Status402PaymentRequired;
            idempotencyRecord.ResponseJson = JsonSerializer.Serialize(paymentFailedResponse, JsonOptions);
            await dbContext.SaveChangesAsync(cancellationToken);

            return StatusCode(StatusCodes.Status402PaymentRequired, paymentFailedResponse);
        }

        try
        {
            if (request.SimulateDatabaseFailure)
            {
                throw new InvalidOperationException("Simulated database failure after payment succeeded.");
            }

            var order = new Order
            {
                UserId = request.UserId,
                CustomerEmail = request.CustomerEmail,
                ShippingAddress = request.ShippingAddress,
                TotalAmount = total,
                Status = OrderStatus.Completed,
                PaymentTransactionId = payment.TransactionId,
                IdempotencyKey = idempotencyKey,
                Items = orderItems
            };

            dbContext.Orders.Add(order);

            var response = order.ToResponse();
            idempotencyRecord.StatusCode = StatusCodes.Status201Created;
            idempotencyRecord.ResponseJson = JsonSerializer.Serialize(response, JsonOptions);

            await dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(OrdersController.GetById),
                "Orders",
                new { id = order.Id },
                response);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Order database save failed after payment succeeded. Transaction {TransactionId} will be refunded.",
                payment.TransactionId);

            await paymentGateway.RefundAsync(
                payment.TransactionId,
                total,
                "Order save failed after successful payment.",
                CancellationToken.None);

            foreach (var entry in dbContext.ChangeTracker.Entries()
                         .Where(entry => entry.State == EntityState.Added && entry.Entity is Order or OrderItem))
            {
                entry.State = EntityState.Detached;
            }

            var failureResponse = new
            {
                message = "Payment succeeded, but saving the order failed. A refund action was triggered.",
                transactionId = payment.TransactionId
            };

            idempotencyRecord.StatusCode = StatusCodes.Status500InternalServerError;
            idempotencyRecord.ResponseJson = JsonSerializer.Serialize(failureResponse, JsonOptions);

            try
            {
                await dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveFailureEx)
            {
                logger.LogError(
                    saveFailureEx,
                    "Failed to persist refund-triggered idempotency response for key {IdempotencyKey}.",
                    idempotencyKey);
            }

            return StatusCode(StatusCodes.Status500InternalServerError, failureResponse);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
