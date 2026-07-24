using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_FiltersByThreshold_AndOrdersByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 15, sku: "SKU-HIGH");
        TestSetup.AddProduct(db, stock: 8, sku: "SKU-MID");
        TestSetup.AddProduct(db, stock: 3, sku: "SKU-LOW");
        // Exactly equal to threshold must be excluded (StockQuantity < threshold).
        TestSetup.AddProduct(db, stock: 10, sku: "SKU-EQ");

        var items = await service.GetLowStockAsync(threshold: 10);

        Assert.Equal(2, items.Count);
        Assert.Equal(new[] { "SKU-LOW", "SKU-MID" }, items.Select(i => i.Sku).ToArray());
        Assert.Equal(new[] { 3, 8 }, items.Select(i => i.StockQuantity).ToArray());
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 2, sku: "SKU-ACTIVE");
        TestSetup.AddProduct(db, stock: 1, sku: "SKU-INACTIVE", isActive: false);

        var items = await service.GetLowStockAsync(threshold: 10);

        Assert.Single(items);
        Assert.Equal("SKU-ACTIVE", items[0].Sku);
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_ExcludesCancelledOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var orderService = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 50, sku: "SKU-SOLD");

        var active = await orderService.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 4) });
        Assert.True(active.Success);

        var toCancel = await orderService.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 7) });
        Assert.True(toCancel.Success);
        var cancelResult = await orderService.CancelOrderAsync(toCancel.Value!.Id);
        Assert.True(cancelResult.Success);

        // Stock after create 50-4-7, then cancel restores 7 → 46
        var items = await service.GetLowStockAsync(threshold: 100);

        var row = Assert.Single(items);
        Assert.Equal(4, row.SoldLast30Days);
        Assert.Equal(46, row.StockQuantity);
    }
}
