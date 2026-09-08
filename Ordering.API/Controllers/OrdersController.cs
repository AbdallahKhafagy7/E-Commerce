using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Services;

namespace Ordering.API.Controllers;

[ApiController]
[Route("api/ordering/orders")]
public sealed class OrdersController(OrderingDbContext dbContext) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

        return order is null ? NotFound() : Ok(order.ToResponse());
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetForUser(string userId, CancellationToken cancellationToken)
    {
        var orders = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(orders.Select(order => order.ToResponse()));
    }
}
