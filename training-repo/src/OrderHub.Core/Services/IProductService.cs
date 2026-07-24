using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();

    /// <summary>
    /// Active products with stock strictly below <paramref name="threshold"/>,
    /// ordered by stock ascending, including sold qty in the last 30 days
    /// (Cancelled orders excluded).
    /// </summary>
    Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(int threshold);
}
