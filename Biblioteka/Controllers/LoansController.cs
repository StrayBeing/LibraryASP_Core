using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Biblioteka.Data;
using Biblioteka.Models;
using Microsoft.AspNetCore.Authorization;

namespace Biblioteka.Controllers
{
    [Authorize(Roles = "Bibliotekarz,Administrator")]
    public class LoansController : Controller
    {
        private readonly LibraryContext _context;

        public LoansController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var loans = await _context.Loans
                .Include(l => l.User)
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .ToListAsync();
            return View(loans);
        }

        public IActionResult Create()
        {
            ViewData["UserID"] = _context.Users
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.UserID.ToString(),
                    Text = $"{u.FirstName} {u.LastName}"
                }).ToList();
            ViewData["CopyID"] = _context.Copies
                .Where(c => c.Available)
                .Include(c => c.Book)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.CopyID.ToString(),
                    Text = $"{c.Book.Title} ({c.CatalogNumber})"
                }).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Loan loan)
        {
            if (ModelState.IsValid)
            {
                var copy = await _context.Copies.FindAsync(loan.CopyID);
                if (copy != null)
                {
                    copy.Available = false;
                    _context.Update(copy);
                }
                _context.Add(loan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserID"] = _context.Users
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.UserID.ToString(),
                    Text = $"{u.FirstName} {u.LastName}"
                }).ToList();
            ViewData["CopyID"] = _context.Copies
                .Where(c => c.Available)
                .Include(c => c.Book)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.CopyID.ToString(),
                    Text = $"{c.Book.Title} ({c.CatalogNumber})"
                }).ToList();
            return View(loan);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var loan = await _context.Loans
                .Include(l => l.User)
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .FirstOrDefaultAsync(l => l.LoanID == id);
            if (loan == null) return NotFound();

            ViewData["UserID"] = _context.Users
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.UserID.ToString(),
                    Text = $"{u.FirstName} {u.LastName}"
                }).ToList();
            ViewData["CopyID"] = _context.Copies
                .Include(c => c.Book)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.CopyID.ToString(),
                    Text = $"{c.Book.Title} ({c.CatalogNumber})"
                }).ToList();
            return View(loan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Loan loan)
        {
            if (id != loan.LoanID) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(loan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserID"] = _context.Users
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.UserID.ToString(),
                    Text = $"{u.FirstName} {u.LastName}"
                }).ToList();
            ViewData["CopyID"] = _context.Copies
                .Include(c => c.Book)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.CopyID.ToString(),
                    Text = $"{c.Book.Title} ({c.CatalogNumber})"
                }).ToList();
            return View(loan);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var loan = await _context.Loans
                .Include(l => l.User)
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .FirstOrDefaultAsync(l => l.LoanID == id);
            if (loan == null) return NotFound();

            return View(loan);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _context.Loans.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }

            _context.Loans.Remove(loan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var loan = await _context.Loans
                .Include(l => l.User)
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .FirstOrDefaultAsync(l => l.LoanID == id);
            if (loan == null) return NotFound();

            return View(loan);
        }
    }

}