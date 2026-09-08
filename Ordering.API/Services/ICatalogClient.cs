namespace Ordering.API.Services;

public interface ICatalogClient
{
    Task<IReadOnlyDictionary<int, CatalogProductSnapshot>> GetProductsAsync(
        IEnumerable<int> productIds,
        CancellationToken cancellationToken);
}
