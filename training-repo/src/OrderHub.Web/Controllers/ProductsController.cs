using Microsoft.AspNetCore.Mvc;
using OrderHub.Core.Services;
using OrderHub.Web.ViewModels;

namespace OrderHub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        var vm = new ProductListViewModel
        {
            Products = products.Select(p => new ProductRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            }).ToList()
        };

        return View(vm);
    }

    /// <summary>
    /// Low-stock alert: GET /Products/LowStock?threshold=10
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LowStock(int? threshold)
    {
        var vm = new LowStockViewModel
        {
            Threshold = threshold ?? LowStockViewModel.DefaultThreshold
        };

        // DataAnnotations + ModelState: invalid threshold must show form error, not 500.
        if (!TryValidateModel(vm))
            return View(vm);

        var items = await _productService.GetLowStockAsync(vm.Threshold);
        vm.Products = items.Select(p => new LowStockRowViewModel
        {
            Sku = p.Sku,
            Name = p.Name,
            StockQuantity = p.StockQuantity,
            SoldLast30Days = p.SoldLast30Days
        }).ToList();

        return View(vm);
    }
}
