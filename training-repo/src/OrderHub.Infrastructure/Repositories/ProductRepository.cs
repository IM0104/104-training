using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderHubDbContext _db;

    public ProductRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<Product>> GetActiveAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Sku).ToListAsync();

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(int threshold, DateTime soldSinceUtc)
    {
        // StockQuantity < threshold (strict), active only, ascending stock.
        var products = await _db.Products
            .Where(p => p.IsActive && p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity)
            .ThenBy(p => p.Sku)
            .Select(p => new { p.Id, p.Sku, p.Name, p.StockQuantity })
            .ToListAsync();

        if (products.Count == 0)
            return Array.Empty<LowStockItem>();

        var productIds = products.Select(p => p.Id).ToList();

        // Single grouped query — avoid N+1 when counting sold quantity.
        var soldByProduct = await _db.OrderItems
            .Where(i => productIds.Contains(i.ProductId)
                        && i.Order!.CreatedAt >= soldSinceUtc
                        && i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        return products
            .Select(p => new LowStockItem
            {
                ProductId = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                StockQuantity = p.StockQuantity,
                SoldLast30Days = soldByProduct.GetValueOrDefault(p.Id)
            })
            .ToList();
    }
}
