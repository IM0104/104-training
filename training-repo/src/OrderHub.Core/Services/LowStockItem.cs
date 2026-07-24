namespace OrderHub.Core.Services;

/// <summary>
/// Active product under a stock threshold, with recent sales for procurement review.
/// </summary>
public class LowStockItem
{
    public int ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public int SoldLast30Days { get; init; }
}
