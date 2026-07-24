using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();
    Task<Product?> GetByIdAsync(int id);
    Task SaveChangesAsync();

    /// <summary>
    /// Active products with StockQuantity &lt; threshold, ordered by stock ascending.
    /// SoldLast30Days counts order lines in the last 30 days, excluding Cancelled orders.
    /// </summary>
    Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(int threshold, DateTime soldSinceUtc);
}
