var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var products = new Dictionary<int, (string Name, decimal Price)>
{
    [1] = ("Laptop", 24999.99m),
    [2] = ("Wireless Mouse", 299.50m),
    [3] = ("Mechanical Keyboard", 1499.00m),
};

app.MapGet("/api/catalog/products/by-ids", (HttpRequest request) =>
{
    var ids = request.Query["ids"]
        .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
        .Where(id => id.HasValue)
        .Select(id => id!.Value)
        .Distinct();

    var result = ids
        .Where(products.ContainsKey)
        .Select(id => new
        {
            productId = id,
            name = products[id].Name,
            price = products[id].Price
        });

    return Results.Ok(result);
});

app.Run("http://localhost:5001");
