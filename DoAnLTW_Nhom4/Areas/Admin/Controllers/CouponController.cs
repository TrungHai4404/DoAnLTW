using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW_Nhom4.Models;
using DoAnLTW_Nhom4.Data;

namespace DoAnLTW_Nhom4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CouponController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Coupon
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Coupons.ToListAsync());
        }

        // GET: Admin/Coupon/Create
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Coupon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public async Task<IActionResult> Create([Bind("Code,DiscountPercentage,ExpiryDate,Description,UsageLimit,MinimumOrderAmount")] Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if coupon code already exists
                    if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
                    {
                        ModelState.AddModelError("Code", "Mã giảm giá đã tồn tại");
                        return View(coupon);
                    }

                    // Set default values
                    coupon.IsActive = true;
                    coupon.UsageCount = 0;

                    _context.Add(coupon);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Thêm mã giảm giá thành công" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi thêm mã giảm giá: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
        }

        // GET: Admin/Coupon/Edit/5
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        // POST: Admin/Coupon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,DiscountPercentage,ExpiryDate,IsActive,Description,UsageLimit,MinimumOrderAmount")] Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(coupon);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Cập nhật mã giảm giá thành công" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CouponExists(coupon.Id))
                    {
                        return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
        }

        // POST: Admin/Coupon/Delete/5
        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin}")]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
            }

            try
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa mã giảm giá thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa mã giảm giá: " + ex.Message });
            }
        }

        // POST: Admin/Coupon/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
            }

            try
            {
                coupon.IsActive = !coupon.IsActive;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message });
            }
        }

        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.Id == id);
        }
    }
} 