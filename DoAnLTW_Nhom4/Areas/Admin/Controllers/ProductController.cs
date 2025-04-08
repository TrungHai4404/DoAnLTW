using DoAnLTW_Nhom4.Data;
using DoAnLTW_Nhom4.Models;
using DoAnLTW_Nhom4.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW_Nhom4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IProductSpecificationRepository _productSpecificationRepository;
        private readonly ILogger<ProductController> _logger;

        public ProductController( ApplicationDbContext context,
                                IProductRepository productRepository, 
                                ICategoryRepository categoryRepository, 
                                IBrandRepository brandRepository, 
                                IProductSpecificationRepository productSpecificationRepository,
                                ILogger<ProductController> logger
                                )
        {
            _context = context;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _productSpecificationRepository = productSpecificationRepository;
            _logger = logger;
        }

        // Hiển thị danh sách tất cả sản phẩm
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }
        // Hiển thị thông tin chi tiết sản phẩm
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Thêm sản phẩm
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var brands = await _brandRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Brands = new SelectList(brands, "Id", "Name");
            return View();
        }


        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public async Task<IActionResult> Add(Product product, IFormFile ImageUrl, List<IFormFile> ImageUrls, List<ProductSpecification> productSpecifications)
        {
            ModelState.Remove("Brand");
            ModelState.Remove("Category");
            if (ModelState.IsValid)
            {
                if (ImageUrl == null)
                {
                    ModelState.AddModelError("ImageUrl", "Vui lòng chọn hình ảnh chính");
                }
                if (ImageUrl != null)
                {
                    if (!IsValidImage(ImageUrl))
                    {
                        ModelState.AddModelError("ImageUrl", "Hình ảnh không hợp lệ");
                        await PrepareViewBags();
                        return View(product);
                    }
                    product.ImageUrl = await SaveImage(ImageUrl);
                }
                if (ImageUrls != null && ImageUrls.Count > 0)
                {
                    foreach (var image in ImageUrls)
                    {
                        if (!IsValidImage(image))
                        {
                            ModelState.AddModelError("ImageUrls", "Hình ảnh không hợp lệ");
                            await PrepareViewBags();
                            return View(product);
                        }
                        var imageUrl = await SaveImage(image);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            product.ImageUrls.Add(new ProductImage { ImageUrl = imageUrl });
                        }
                    }
                }

                // Thêm thông số kỹ thuật vào sản phẩm trước khi lưu
                if (productSpecifications != null && productSpecifications.Any())
                {
                    product.ProductSpecifications = productSpecifications;
                }

                await _productRepository.AddAsync(product);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            await PrepareViewBags();
            return View(product);
        }

        // Kiểm tra hình ảnh hợp lệ

        private bool IsValidImage(IFormFile file)
        {
            var validImageTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            var maxFileSize = 1 * 1024 * 1024; // 1MB

            if (!validImageTypes.Contains(file.ContentType) || file.Length > maxFileSize)
            {
                return false;
            }
            return true;
        }

        // Lưu hình ảnh
        private async Task<string> SaveImage(IFormFile image)
        {
            var savePath = Path.Combine("wwwroot/images", image.FileName); // Thay đổi đường dẫn theo cấu hình của bạn

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            return "/images/" + image.FileName; // Trả về đường dẫn tương đối
        }


        // Hiển thị sản phẩm theo danh mục
        public async Task<IActionResult> ByCategory(int categoryId)
        {
            var products = await _productRepository.GetByCategoryAsync(categoryId);
            return View("Index", products); // Sử dụng lại view Index
        }

        // Hiển thị sản phẩm theo thương hiệu
        public async Task<IActionResult> ByBrand(int brandId)
        {
            var products = await _productRepository.GetByBrandAsync(brandId);
            return View("Index", products); // Sử dụng lại view Index
        }

        [HttpGet]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductSpecifications) // Load thông số kỹ thuật
                .Include(p => p.ImageUrls)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", product.BrandId);

            return View(product);
        }


        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Edit(Product product, IFormFile? ImageUrl, List<IFormFile>? ImageUrls, List<ProductSpecification>? ProductSpecifications)
        {
            ModelState.Remove("Brand");
            ModelState.Remove("Category");
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
                ViewBag.Brands = new SelectList(await _brandRepository.GetAllAsync(), "Id", "Name");
                return View(product);
            }

            var existingProduct = await _productRepository.GetByIdAsync(product.Id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Discount = product.Discount;
            existingProduct.Stock = product.Stock;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.BrandId = product.BrandId;
            existingProduct.Description = product.Description;

            // Cập nhật hình ảnh chính
            if (ImageUrl != null)
            {
                if (!IsValidImage(ImageUrl))
                {
                    ModelState.AddModelError("ImageUrl", "Hình ảnh không hợp lệ");
                    return View(product);
                }
                existingProduct.ImageUrl = await SaveImage(ImageUrl); // Lưu ảnh mới
            }
            //Nếu không có hình ảnh mới thì giữ lại hình ảnh cũ
            else
            {
                ModelState.Remove("ImageUrl");
            }

            // Xử lý hình ảnh phụ
            // Lấy danh sách các URL ảnh cũ
            var oldImageUrls = existingProduct.ImageUrls.Select(img => img.ImageUrl).ToList();

            // Xử lý hình ảnh phụ mới
            if (ImageUrls != null && ImageUrls.Count > 0)
            {
                foreach (var image in ImageUrls)
                {
                    if (!IsValidImage(image))
                    {
                        ModelState.AddModelError("ImageUrls", "Hình ảnh không hợp lệ");
                        return View(product);
                    }

                    var imageUrl = await SaveImage(image);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // Kiểm tra xem URL ảnh mới đã tồn tại trong danh sách ảnh cũ chưa
                        if (!oldImageUrls.Contains(imageUrl))
                        {
                            existingProduct.ImageUrls.Add(new ProductImage { ImageUrl = imageUrl });
                        }
                        
                    }
                }
            }

            // Cập nhật thông số kỹ thuật
            existingProduct.ProductSpecifications.Clear(); // Xóa danh sách cũ
            if (ProductSpecifications != null && ProductSpecifications.Any())
            {
                foreach (var spec in ProductSpecifications)
                {
                    existingProduct.ProductSpecifications.Add(new ProductSpecification
                    {
                        Key = spec.Key,
                        Value = spec.Value
                    });
                }
            }

            await _productRepository.UpdateAsync(existingProduct);
            return RedirectToAction(nameof(Index));
        }

        // GET: Delete
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveImage(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image != null)
            {
                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        private async Task PrepareViewBags()
        {
            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
            ViewBag.Brands = new SelectList(await _brandRepository.GetAllAsync(), "Id", "Name");
        }
    }
}