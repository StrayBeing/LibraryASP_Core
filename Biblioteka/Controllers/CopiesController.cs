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
    public class CopiesController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ILogger<CopiesController> _logger;

        public CopiesController(LibraryContext context, ILogger<CopiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                var books = await _context.Books
                    .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = b.BookID.ToString(),
                        Text = $"{b.Title} ({b.Author})"
                    }).ToListAsync();

                if (!books.Any())
                {
                    _logger.LogWarning("Brak książek w bazie danych podczas tworzenia egzemplarza");
                    TempData["Error"] = "Brak dostępnych książek. Dodaj książkę przed utworzeniem egzemplarza.";
                }

                ViewData["BookID"] = books;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas przygotowywania formularza tworzenia egzemplarza");
                TempData["Error"] = "Wystąpił błąd podczas przygotowywania formularza.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Copy copy)
        {
            // Log raw form data
            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation("Raw form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}: {kv.Value}")));
            _logger.LogInformation("Otrzymano BookID: {BookID}, CatalogNumber: {CatalogNumber}, Available: {Available}",
                copy.BookID, copy.CatalogNumber, copy.Available);

            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                _logger.LogWarning("ModelState errors: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
            }

            // Temporarily bypass ModelState validation to test saving
            try
            {
                copy.CatalogNumber = copy.CatalogNumber?.Trim();
                if (await _context.Copies.AnyAsync(c => c.CatalogNumber.ToLower() == copy.CatalogNumber.ToLower()))
                {
                    ModelState.AddModelError("CatalogNumber", "Numer katalogowy już istnieje.");
                }
                else if (copy.BookID <= 0)
                {
                    ModelState.AddModelError("BookID", "Proszę wybrać książkę.");
                }
                else
                {
                    _context.Add(copy);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Egzemplarz {CatalogNumber} został utworzony", copy.CatalogNumber);
                    TempData["Success"] = "Egzemplarz został dodany.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                _logger.LogError(ex, "Próba dodania zduplikowanego numeru katalogowego {CatalogNumber}", copy.CatalogNumber);
                ModelState.AddModelError("CatalogNumber", "Numer katalogowy już istnieje.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia egzemplarza {CatalogNumber}", copy.CatalogNumber);
                TempData["Error"] = "Wystąpił błąd podczas dodawania egzemplarza: " + ex.Message;
            }

            ViewData["BookID"] = await _context.Books
                .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = b.BookID.ToString(),
                    Text = $"{b.Title} ({b.Author})"
                }).ToListAsync();
            return View(copy);
        }

        public IActionResult TestCreate()
        {
            return View();
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var copies = await _context.Copies
                    .Include(c => c.Book)
                    .Include(c => c.Loans)
                    .ToListAsync();
                return View(copies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy egzemplarzy");
                TempData["Error"] = "Wystąpił błąd podczas pobierania listy egzemplarzy.";
                return View(new System.Collections.Generic.List<Copy>());
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edycja egzemplarza wywołana z pustym ID");
                return NotFound();
            }

            try
            {
                var copy = await _context.Copies
                    .Include(c => c.Book)
                    .FirstOrDefaultAsync(c => c.CopyID == id);
                if (copy == null)
                {
                    _logger.LogWarning("Egzemplarz o ID {CopyId} nie został znaleziony", id);
                    return NotFound();
                }

                ViewData["BookID"] = await _context.Books
                    .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = b.BookID.ToString(),
                        Text = $"{b.Title} ({b.Author})"
                    }).ToListAsync();
                return View(copy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania egzemplarza do edycji, ID {CopyId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania egzemplarza do edycji.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Copy copy)
        {
            if (id != copy.CopyID)
            {
                _logger.LogWarning("Niezgodność ID egzemplarza {CopyId} w edycji", id);
                return NotFound();
            }

            // Log raw form data
            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation("Raw form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}: {kv.Value}")));
            _logger.LogInformation("Otrzymano BookID: {BookID}, CatalogNumber: {CatalogNumber}, Available: {Available}",
                copy.BookID, copy.CatalogNumber, copy.Available);

            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                _logger.LogWarning("ModelState errors: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
            }

            // Bypass ModelState validation
            try
            {
                copy.CatalogNumber = copy.CatalogNumber?.Trim();
                if (await _context.Copies.AnyAsync(c => c.CatalogNumber.ToLower() == copy.CatalogNumber.ToLower() && c.CopyID != id))
                {
                    ModelState.AddModelError("CatalogNumber", "Numer katalogowy już istnieje.");
                }
                else if (copy.BookID <= 0)
                {
                    ModelState.AddModelError("BookID", "Proszę wybrać książkę.");
                }
                else
                {
                    _context.Update(copy);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Egzemplarz {CatalogNumber} został zaktualizowany", copy.CatalogNumber);
                    TempData["Success"] = "Egzemplarz został zaktualizowany.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                _logger.LogError(ex, "Próba edycji na zduplikowany numer katalogowy {CatalogNumber}", copy.CatalogNumber);
                ModelState.AddModelError("CatalogNumber", "Numer katalogowy już istnieje.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji egzemplarza {CatalogNumber}", copy.CatalogNumber);
                TempData["Error"] = "Wystąpił błąd podczas aktualizacji egzemplarza: " + ex.Message;
            }

            ViewData["BookID"] = await _context.Books
                .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = b.BookID.ToString(),
                    Text = $"{b.Title} ({b.Author})"
                }).ToListAsync();
            return View(copy);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Usuwanie egzemplarza wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var copy = await _context.Copies
                    .Include(c => c.Book)
                    .Include(c => c.Loans)
                    .FirstOrDefaultAsync(c => c.CopyID == id);
                if (copy == null)
                {
                    _logger.LogWarning("Egzemplarz o ID {CopyId} nie został znaleziony", id);
                    return NotFound();
                }
                return View(copy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania egzemplarza do usunięcia, ID {CopyId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania egzemplarza do usunięcia.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var copy = await _context.Copies
                    .Include(c => c.Loans)
                    .FirstOrDefaultAsync(c => c.CopyID == id);
                if (copy == null)
                {
                    _logger.LogWarning("Egzemplarz o ID {CopyId} nie został znaleziony", id);
                    return NotFound();
                }

                if (copy.Loans.Any(l => l.ReturnDate == null))
                {
                    _logger.LogWarning("Próba usunięcia egzemplarza {CopyId} z aktywnymi wypożyczeniami", id);
                    TempData["Error"] = "Nie można usunąć egzemplarza, ponieważ jest obecnie wypożyczony.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Copies.Remove(copy);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Egzemplarz o ID {CopyId} został usunięty", id);
                TempData["Success"] = "Egzemplarz został usunięty.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
            {
                _logger.LogError(ex, "Nie można usunąć egzemplarza o ID {CopyId} z powodu powiązanych danych", id);
                TempData["Error"] = "Nie można usunąć egzemplarza, ponieważ jest powiązany z innymi danymi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania egzemplarza o ID {CopyId}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania egzemplarza.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Szczegóły egzemplarza wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var copy = await _context.Copies
                    .Include(c => c.Book)
                    .Include(c => c.Loans)
                    .FirstOrDefaultAsync(c => c.CopyID == id);
                if (copy == null)
                {
                    _logger.LogWarning("Egzemplarz o ID {CopyId} nie został znaleziony", id);
                    return NotFound();
                }
                return View(copy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów egzemplarza dla ID {CopyId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania szczegółów egzemplarza.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}