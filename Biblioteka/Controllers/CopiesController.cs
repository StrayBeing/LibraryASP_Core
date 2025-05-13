using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Biblioteka.Data;
using Biblioteka.Models;
using Microsoft.AspNetCore.Authorization;

namespace Biblioteka.Controllers
{
    [Authorize(Roles = "Bibliotekarz,Administrator")]
    public class CopiesController : Controller
    {
        private readonly LibraryContext _context;

        public CopiesController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var copies = await _context.Copies
                .Include(c => c.Book)
                .ToListAsync();
            return View(copies);
        }

        public IActionResult Create()
        {
            ViewData["BookID"] = _context.Books
                .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = b.BookID.ToString(),
                    Text = b.Title
                }).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Copy copy)
        {
            if (ModelState.IsValid)
            {
                _context.Add(copy);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookID"] = _context.Books
                .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = b.BookID.ToString(),
                    Text = b.Title
                }).ToList();
            return View(copy);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var copy = await _context.Copies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.CopyID == id);
            if (copy == null) return NotFound();

            ViewData["BookID"] = _context.Books
                .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = b.BookID.ToString(),
                    Text = b.Title
                }).ToList();
            return View(copy);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Copy copy)
        {
            if (id != copy.CopyID) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(copy);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookID"] = _context.Books
                .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = b.BookID.ToString(),
                    Text = b.Title
                }).ToList();
            return View(copy);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var copy = await _context.Copies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.CopyID == id);
            if (copy == null) return NotFound();

            return View(copy);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var copy = await _context.Copies.FindAsync(id);
            if (copy == null)
            {
                return NotFound();
            }

            _context.Copies.Remove(copy);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var copy = await _context.Copies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.CopyID == id);
            if (copy == null) return NotFound();

            return View(copy);
        }
    }

}