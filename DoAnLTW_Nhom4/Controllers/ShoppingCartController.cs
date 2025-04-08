using DoAnLTW_Nhom4.Extensions;
using DoAnLTW_Nhom4.Models;
using DoAnLTW_Nhom4.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW_Nhom4.Data;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace DoAnLTW_Nhom4.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ShoppingCartController> _logger;
        private readonly IEmailSender _emailSender;

        public ShoppingCartController(
            IProductRepository productRepository, 
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<ShoppingCartController> logger,
            IEmailSender emailSender)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }
        [HttpPost]
        public async Task<IActionResult> ValidateCoupon(string code, decimal totalAmount)
        {
            _logger.LogInformation($"Validating coupon: {code} for total amount: {totalAmount}");

            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá" });
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code && c.IsActive);

            if (coupon == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hiệu lực" });
            }

            if (coupon.ExpiryDate < DateTime.Now)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn" });
            }

            if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng" });
            }

            if (coupon.MinimumOrderAmount.HasValue && totalAmount < coupon.MinimumOrderAmount.Value)
            {
                return Json(new { success = false, message = $"Đơn hàng phải có giá trị tối thiểu {coupon.MinimumOrderAmount.Value:N0}đ" });
            }

            // Calculate discount amount
            decimal discountAmount = totalAmount * (coupon.DiscountPercentage / 100);
            decimal finalAmount = totalAmount - discountAmount;

            _logger.LogInformation($"Calculated discount: {discountAmount}, Final amount: {finalAmount}");

            // Lưu thông tin mã giảm giá vào Session
            HttpContext.Session.SetInt32("AppliedCouponId", coupon.Id);
            HttpContext.Session.SetDecimal("DiscountAmount", discountAmount);
            HttpContext.Session.SetDecimal("FinalAmount", finalAmount);

            _logger.LogInformation($"Saved to session - CouponId: {coupon.Id}, DiscountAmount: {discountAmount}, FinalAmount: {finalAmount}");

            // Cập nhật đơn hàng tạm thời trong Session
            var pendingOrder = HttpContext.Session.GetObjectFromJson<Order>("PendingOrder");
            if (pendingOrder != null)
            {
                pendingOrder.CouponId = coupon.Id;
                pendingOrder.DiscountAmount = discountAmount;
                pendingOrder.FinalAmount = finalAmount;
                HttpContext.Session.SetObjectAsJson("PendingOrder", pendingOrder);
                _logger.LogInformation($"Updated pending order with discount information");
            }
            else
            {
                _logger.LogWarning("No pending order found in session");
            }

            return Json(new
            {
                success = true,
                message = "Áp dụng mã giảm giá thành công",
                coupon = new
                {
                    id = coupon.Id,
                    code = coupon.Code,
                    discountPercentage = coupon.DiscountPercentage,
                    discountAmount = discountAmount,
                    finalAmount = finalAmount
                }
            });
        }
        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Customer}")]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            try
            {
                _logger.LogInformation($"Adding product {productId} to cart with quantity {quantity}");
                
                if (productId <= 0)
                {
                    _logger.LogWarning($"Invalid product ID: {productId}");
                    return Json(new { success = false, message = "ID sản phẩm không hợp lệ" });
                }

                if (quantity <= 0)
                {
                    _logger.LogWarning($"Invalid quantity: {quantity}");
                    return Json(new { success = false, message = "Số lượng sản phẩm không hợp lệ" });
                }

                var product = await GetProductFromDatabase(productId);
                
                if (product == null)
                {
                    _logger.LogWarning($"Product not found: {productId}");
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                if (product.Stock < quantity)
                {
                    _logger.LogWarning($"Insufficient stock for product {productId}. Available: {product.Stock}, Requested: {quantity}");
                    return Json(new { success = false, message = $"Số lượng sản phẩm trong kho không đủ. Còn lại: {product.Stock}" });
                }

                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
                var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

                if (cartItem != null)
                {
                    if (product.Stock < cartItem.Quantity + quantity)
                    {
                        _logger.LogWarning($"Insufficient stock for product {productId} when updating cart. Available: {product.Stock}, Current: {cartItem.Quantity}, Adding: {quantity}");
                        return Json(new { success = false, message = $"Số lượng sản phẩm trong kho không đủ. Còn lại: {product.Stock}" });
                    }
                    cartItem.Quantity += quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductId = productId,
                        Name = product.Name,
                        ImageUrl = product.ImageUrl ?? string.Empty,
                        Price = (product.Price * (1 -product.Discount/100)),
                        Quantity = quantity,
                        StockQuantity = product.Stock
                    });
                }

                HttpContext.Session.SetObjectAsJson("Cart", cart);
                _logger.LogInformation($"Successfully added product {productId} to cart with quantity {quantity}");
                
                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Error adding product {productId} to cart: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error adding product {productId} to cart: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng" });
            }
        }

        [Authorize]
        [Authorize(Roles = $"{SD.Role_Customer}")]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            return View(cart);
        }

        private async Task<Product> GetProductFromDatabase(int productId)
        {
            try
            {
                _logger.LogInformation($"Attempting to find product with ID: {productId}");
                
                // Kiểm tra xem productId có hợp lệ không
                if (productId <= 0)
                {
                    _logger.LogWarning($"Invalid product ID: {productId}");
                    throw new InvalidOperationException($"ID sản phẩm không hợp lệ: {productId}");
                }

                // Sử dụng repository để lấy sản phẩm
                var product = await _productRepository.GetByIdAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning($"Product not found with ID: {productId}");
                    throw new InvalidOperationException($"Không tìm thấy sản phẩm với ID: {productId}");
                }

                _logger.LogInformation($"Successfully found product: {product.Name} (ID: {product.Id})");
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving product with ID {productId}: {ex.Message}");
                throw new InvalidOperationException($"Có lỗi xảy ra khi tìm kiếm sản phẩm: {ex.Message}");
            }
        }

        // Xoá sản phẩm khỏi giỏ hàng
        [Authorize(Roles = $"{SD.Role_Customer}")]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart is not null)
            {
                cart.RemoveItem(productId);
                // Lưu lại giỏ hàng vào Session sau khi đã xóa mục
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }
        // Xóa tất cả sản phẩm khỏi giỏ hàng
        [Authorize(Roles = $"{SD.Role_Customer}")]
        public IActionResult RemoveAllFromCart()
        {
            // Lấy giỏ hàng từ session
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                // Xóa tất cả sản phẩm trong giỏ hàng
                cart.Items.Clear();
                // Cập nhật giỏ hàng vào session
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }
        public IActionResult Checkout()
        {
            return View(new Order());
        }
        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Customer}")]
        public async Task<IActionResult> Checkout(Order order, List<int> selectedProductIds)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            var pendingOrder = HttpContext.Session.GetObjectFromJson<Order>("PendingOrder");

            if (cart == null || pendingOrder == null || !pendingOrder.OrderDetails.Any())
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra, vui lòng thử lại!";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thanh toán!";
                return RedirectToAction("Login");
            }

            // Cập nhật thông tin đơn hàng
            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.Status = OrderStatus.Pending;
            order.TotalPrice = pendingOrder.TotalPrice;
            order.OrderDetails = pendingOrder.OrderDetails;

            // Cập nhật thông tin giảm giá nếu có
            if (order.CouponId.HasValue)
            {
                var coupon = await _context.Coupons.FindAsync(order.CouponId.Value);
                if (coupon != null)
                {
                    order.DiscountAmount = order.TotalPrice * (coupon.DiscountPercentage / 100);
                    order.FinalAmount = order.TotalPrice - order.DiscountAmount;

                    // Giảm số lần sử dụng
                    coupon.UsageCount = Math.Max(coupon.UsageCount - 1, 0);

                    _context.Coupons.Update(coupon); // Cập nhật coupon
                    await _context.SaveChangesAsync(); // Lưu thay đổi vào cơ sở dữ liệu
                }
            }
            else
            {
                order.DiscountAmount = 0;
                order.FinalAmount = order.TotalPrice;
            }

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                // ✅ Gửi email xác nhận đơn hàng
                var subject = $"Xác nhận đơn hàng #{order.Id}";
                var body = $@"
                            <h3>Xin chào {user.FullName},</h3>
                            <p>Cảm ơn bạn đã đặt hàng tại cửa hàng của chúng tôi.</p>
                            <p><strong>Mã đơn hàng:</strong> {order.Id}</p>
                            <p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Tổng tiền:</strong> {order.TotalPrice:N0}đ</p>
                            <p><strong>Giảm giá:</strong> {order.DiscountAmount:N0}đ</p>
                            <p><strong>Thanh toán:</strong> <span style='color: green; font-weight: bold'>{order.FinalAmount:N0}đ</span></p>
                            <p>Chúng tôi sẽ sớm liên hệ với bạn để xác nhận và giao hàng.</p>
                            <br/>
                            <p>Trân trọng,</p>
                            <p><em>Đội ngũ hỗ trợ</em></p>
                        ";

                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi khi xử lý đơn hàng, vui lòng thử lại!";
                return RedirectToAction("Index");
            }
            // Cập nhật số lượng sản phẩm trong kho
            foreach (var orderDetail in order.OrderDetails)
            {
                var product = await _context.Products.FindAsync(orderDetail.ProductId);
                if (product != null && product.Stock >= orderDetail.Quantity)
                {
                    // Giảm số lượng sản phẩm trong kho
                    product.Stock -= orderDetail.Quantity;
                    _context.Products.Update(product);  // Lưu thay đổi vào cơ sở dữ liệu
                    await _context.SaveChangesAsync();
                }
            }
            // Xóa sản phẩm đã thanh toán khỏi giỏ hàng
            cart.Items = cart.Items.Where(i => !selectedProductIds.Contains(i.ProductId)).ToList();
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            

            // Xóa đơn hàng tạm thời khỏi Session
            HttpContext.Session.Remove("PendingOrder");

            return RedirectToAction("OrderCompleted", new { id = order.Id });
        }


        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Customer}")]
        public IActionResult ProceedToCheckout(List<int> selectedProductIds)
        {
            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán!";
                return RedirectToAction("Index"); // Quay lại giỏ hàng
            }

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || cart.Items == null || cart.Items.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index");
            }

            var selectedItems = cart.Items.Where(i => selectedProductIds.Contains(i.ProductId)).ToList();
            if (!selectedItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm hợp lệ để thanh toán!";
                return RedirectToAction("Index");
            }

            // Lấy thông tin mã giảm giá từ Session nếu có
            var couponId = HttpContext.Session.GetInt32("AppliedCouponId");
            var discountAmount = HttpContext.Session.GetDecimal("DiscountAmount");
            var finalAmount = HttpContext.Session.GetDecimal("FinalAmount");

            var totalPrice = selectedItems.Sum(i => i.Price * i.Quantity);

            // Tạo đơn hàng tạm thời để hiển thị trên trang Checkout
            var order = new Order
            {
                OrderDetails = selectedItems.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    Subtotal = i.Price * i.Quantity
                }).ToList(),
                TotalPrice = totalPrice,
                FullName = "",
                PhoneNumber = "",
                CouponId = couponId,
                DiscountAmount = discountAmount ?? 0,
                FinalAmount = finalAmount ?? totalPrice
            };

            // Lưu đơn hàng tạm thời vào Session
            HttpContext.Session.SetObjectAsJson("PendingOrder", order);
            HttpContext.Session.SetObjectAsJson("SelectedProductIds", selectedProductIds);

            Console.WriteLine("Selected Product IDs: " + string.Join(", ", selectedProductIds));

            return View("Checkout", order);
        }
        public async Task<IActionResult> OrderCompleted(int id)
        {
            var order = await _context.Orders
               .Include(o => o.OrderDetails)
                   .ThenInclude(od => od.Product)
               .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
        [HttpPost]
        public JsonResult UpdateCart([FromBody] CartUpdateRequest request)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (item != null)
                {
                    item.Quantity = request.Quantity;
                }

                // 🔥 Lưu giỏ hàng vào Session
                HttpContext.Session.SetObjectAsJson("Cart", cart);

                return Json(new { success = true, totalPrice = cart.Items.Sum(i => i.Price * i.Quantity) });
            }

            return Json(new { success = false, message = "Giỏ hàng không tồn tại" });
        }

        // 🛒 Tạo class để nhận dữ liệu từ AJAX
        public class CartUpdateRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public IActionResult OrderDetails(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product) // Nạp sản phẩm để tránh null
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        public IActionResult OrderList()
        {
            var userId = _userManager.GetUserId(User); // Lấy ID của người dùng đang đăng nhập

            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }
    }
}
