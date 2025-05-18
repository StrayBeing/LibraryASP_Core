using Biblioteka.Data;
using Biblioteka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Biblioteka.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(LibraryContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.BookCategories)
                    .ThenInclude(bc => bc.Book)
                    .ToListAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy kategorii");
                TempData["Error"] = "Wystąpił błąd podczas pobierania listy kategorii.";
                return View(new System.Collections.Generic.List<Category>());
            }
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Categories.AnyAsync(c => c.Name == category.Name))
                    {
                        ModelState.AddModelError("Name", "Kategoria o tej nazwie już istnieje.");
                        return View(category);
                    }

                    _context.Add(category);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Kategoria {CategoryName} została utworzona", category.Name);
                    TempData["Success"] = "Kategoria została dodana.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas tworzenia kategorii {CategoryName}", category.Name);
                    TempData["Error"] = "Wystąpił błąd podczas dodawania kategorii.";
                }
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edycja kategorii wywołana z pustym ID");
                return NotFound();
            }

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Kategoria o ID {CategoryId} nie została znaleziona", id);
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania kategorii do edycji, ID {CategoryId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania kategorii do edycji.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.CategoryID)
            {
                _logger.LogWarning("Niezgodność ID kategorii {CategoryId} w edycji", id);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Categories.AnyAsync(c => c.Name == category.Name && c.CategoryID != id))
                    {
                        ModelState.AddModelError("Name", "Kategoria o tej nazwie już istnieje.");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Kategoria {CategoryName} została zaktualizowana", category.Name);
                    TempData["Success"] = "Kategoria została zaktualizowana.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas aktualizacji kategorii {CategoryName}", category.Name);
                    TempData["Error"] = "Wystąpił błąd podczas aktualizacji kategorii.";
                }
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Usuwanie kategorii wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var category = await _context.Categories
                    .Include(c => c.BookCategories)
                    .ThenInclude(bc => bc.Book)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);
                if (category == null)
                {
                    _logger.LogWarning("Kategoria o ID {CategoryId} nie została znaleziona", id);
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania kategorii do usunięcia, ID {CategoryId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania kategorii do usunięcia.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Kategoria o ID {CategoryId} nie została znaleziona", id);
                    return NotFound();
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Kategoria o ID {CategoryId} została usunięta", id);
                TempData["Success"] = "Kategoria została usunięta.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
            {
                _logger.LogError(ex, "Nie można usunąć kategorii o ID {CategoryId} z powodu powiązanych książek", id);
                TempData["Error"] = "Nie można usunąć kategorii, ponieważ jest powiązana z książkami.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania kategorii o ID {CategoryId}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania kategorii.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Szczegóły kategorii wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var category = await _context.Categories
                    .Include(c => c.BookCategories)
                    .ThenInclude(bc => bc.Book)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);
                if (category == null)
                {
                    _logger.LogWarning("Kategoria o ID {CategoryId} nie została znaleziona", id);
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów kategorii dla ID {CategoryId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania szczegółów kategorii.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}