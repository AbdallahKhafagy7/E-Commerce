using System.Net.Http.Json;

namespace Ordering.API.Services;

public sealed class HttpCatalogClient(HttpClient httpClient, ILogger<HttpCatalogClient> logger) : ICatalogClient
{
    public async Task<IReadOnlyDictionary<int, CatalogProductSnapshot>> GetProductsAsync(
        IEnumerable<int> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().OrderBy(id => id).ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, CatalogProductSnapshot>();
        }

        var query = string.Join("&", ids.Select(id => $"ids={id}"));
        var products = await httpClient.GetFromJsonAsync<List<CatalogProductSnapshot>>(
            $"api/catalog/products/by-ids?{query}",
            cancellationToken);

        if (products is null)
        {
            logger.LogWarning("Catalog returned an empty response for product IDs {ProductIds}", ids);
            return new Dictionary<int, CatalogProductSnapshot>();
        }

        return products.ToDictionary(product => product.ProductId);
    }
}
