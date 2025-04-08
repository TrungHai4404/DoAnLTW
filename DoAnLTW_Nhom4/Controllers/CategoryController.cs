using DoAnLTW_Nhom4.Data;
using DoAnLTW_Nhom4.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW_Nhom4.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public CategoryController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = categories;

            var products = await _productRepository.GetAllAsync();
            if (products == null || !products.Any())
            {
                // Debugging
                Console.WriteLine("Không có sản phẩm nào.");
            }
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> FilterByCategory(int? categoryId)
        {
            var products = categoryId == null
                ? await _productRepository.GetAllAsync()
                : await _productRepository.GetByCategoryAsync(categoryId.Value);

            return PartialView("_ProductListPartial", products);
        }
    }

}