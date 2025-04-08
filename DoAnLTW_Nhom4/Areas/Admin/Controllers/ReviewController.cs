using DoAnLTW_Nhom4.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW_Nhom4.Models;
using Microsoft.AspNetCore.Authorization;
[Area("Admin")]
[Authorize]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Admin/Reviews
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public async Task<IActionResult> Index()
    {
        var reviews = await _context.Reviews.Include(r => r.Product).ToListAsync();
        return View(reviews);
    }

    // GET: Admin/Reviews/Details/5
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var review = await _context.Reviews.Include(r => r.Product)
                          .FirstOrDefaultAsync(m => m.Id == id);
        if (review == null) return NotFound();

        return View(review);
    }

    // POST: Admin/Reviews/Approve/5
    [HttpPost]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public async Task<IActionResult> Approve(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound();

        review.IsApproved = true;
        review.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Reviews/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{SD.Role_Admin}")]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
