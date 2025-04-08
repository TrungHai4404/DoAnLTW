using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW_Nhom4.Data;
using DoAnLTW_Nhom4.Models;
using DoAnLTW_Nhom4.Repositories.Interfaces;

namespace DoAnLTW_Nhom4.Areas.Admin.Controllers
{
    
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderRepository orderRepository, ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        // GET: Admin/Order
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _orderRepository.GetAllAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching orders");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn hàng";
                return View(new List<Order>());
            }
        }

        // GET: Admin/Order/Details/5
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(id);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Index));
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching order details");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết đơn hàng";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Order/UpdateStatus/5
        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                order.Status = newStatus;
                await _orderRepository.UpdateAsync(order);

                return Json(new { success = true, message = "Đã cập nhật trạng thái đơn hàng" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating order status");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng" });
            }
        }

        // GET: Admin/Order/Delete/5
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(id);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Index));
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching order for deletion");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn hàng";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Order/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(id);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Index));
                }

                await _orderRepository.DeleteAsync(order.Id);
                TempData["SuccessMessage"] = "Đã xóa đơn hàng thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting order");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa đơn hàng";
                return RedirectToAction(nameof(Index));
            }
        }
        // GET: /Admin/Order/Pending
        public async Task<IActionResult> Pending()
        {
            var pendingOrders = await _orderRepository.GetPendingOrders();
            return View(pendingOrders);
        }
    }
} 