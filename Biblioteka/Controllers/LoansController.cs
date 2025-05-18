using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Biblioteka.Data;
using Biblioteka.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Biblioteka.Controllers
{
    public class LoansController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ILogger<LoansController> _logger;

        public LoansController(LibraryContext context, ILogger<LoansController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var loans = await _context.Loans
                    .Include(l => l.User)
                    .Include(l => l.Copy)
                    .ThenInclude(c => c.Book)
                    .ToListAsync();
                return View(loans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy wypożyczeń");
                TempData["Error"] = "Wystąpił błąd podczas pobierania listy wypożyczeń.";
                return View(new System.Collections.Generic.List<Loan>());
            }
        }

        public IActionResult Create()
        {
            try
            {
                var users = _context.Users
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserID.ToString(),
                        Text = $"{u.FirstName} {u.LastName}"
                    }).ToList();
                var copies = _context.Copies
                    .Where(c => c.Available)
                    .Include(c => c.Book)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CopyID.ToString(),
                        Text = $"{c.Book.Title} ({c.CatalogNumber})"
                    }).ToList();

                if (!users.Any() || !copies.Any())
                {
                    _logger.LogWarning("Brak użytkowników lub dostępnych egzemplarzy podczas tworzenia wypożyczenia");
                    TempData["Error"] = "Brak dostępnych użytkowników lub egzemplarzy.";
                }

                ViewData["UserID"] = users;
                ViewData["CopyID"] = copies;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas przygotowywania formularza tworzenia wypożyczenia");
                TempData["Error"] = "Wystąpił błąd podczas przygotowywania formularza.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Loan loan)
        {
            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation("Raw form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}: {kv.Value}")));
            _logger.LogInformation("Otrzymano UserID: {UserID}, CopyID: {CopyID}, DueDate: {DueDate}, ReturnDate: {ReturnDate}",
                loan.UserID, loan.CopyID, loan.DueDate, loan.ReturnDate);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                _logger.LogWarning("ModelState errors: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
            }

            ModelState.Clear();

            try
            {
                if (loan.UserID <= 0)
                {
                    ModelState.AddModelError("UserID", "Proszę wybrać użytkownika.");
                }
                if (loan.CopyID <= 0)
                {
                    ModelState.AddModelError("CopyID", "Proszę wybrać egzemplarz.");
                }
                if (loan.DueDate == default)
                {
                    ModelState.AddModelError("DueDate", "Data zwrotu jest wymagana.");
                }

                var copy = await _context.Copies.FindAsync(loan.CopyID);
                if (copy == null || !copy.Available)
                {
                    ModelState.AddModelError("CopyID", "Wybrany egzemplarz nie jest dostępny.");
                }

                var user = await _context.Users.FindAsync(loan.UserID);
                if (user == null)
                {
                    ModelState.AddModelError("UserID", "Wybrany użytkownik nie istnieje.");
                }

                if (ModelState.ErrorCount > 0)
                {
                    ViewData["UserID"] = _context.Users
                        .Select(u => new SelectListItem
                        {
                            Value = u.UserID.ToString(),
                            Text = $"{u.FirstName} {u.LastName}"
                        }).ToList();
                    ViewData["CopyID"] = _context.Copies
                        .Where(c => c.Available)
                        .Include(c => c.Book)
                        .Select(c => new SelectListItem
                        {
                            Value = c.CopyID.ToString(),
                            Text = $"{c.Book.Title} ({c.CatalogNumber})"
                        }).ToList();
                    return View(loan);
                }

                copy.Available = false;
                loan.LoanDate = DateTime.Now;
                _context.Update(copy);
                _context.Add(loan);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Wypożyczenie {LoanID} zostało utworzone", loan.LoanID);
                TempData["Success"] = "Wypożyczenie zostało dodane.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia wypożyczenia");
                TempData["Error"] = "Wystąpił błąd podczas dodawania wypożyczenia: " + ex.Message;
                ViewData["UserID"] = _context.Users
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserID.ToString(),
                        Text = $"{u.FirstName} {u.LastName}"
                    }).ToList();
                ViewData["CopyID"] = _context.Copies
                    .Where(c => c.Available)
                    .Include(c => c.Book)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CopyID.ToString(),
                        Text = $"{c.Book.Title} ({c.CatalogNumber})"
                    }).ToList();
                return View(loan);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edycja wypożyczenia wywołana z pustym ID");
                return NotFound();
            }

            try
            {
                var loan = await _context.Loans
                    .Include(l => l.User)
                    .Include(l => l.Copy)
                    .ThenInclude(c => c.Book)
                    .FirstOrDefaultAsync(l => l.LoanID == id);
                if (loan == null)
                {
                    _logger.LogWarning("Wypożyczenie o ID {LoanID} nie zostało znalezione", id);
                    return NotFound();
                }

                ViewData["UserID"] = _context.Users
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserID.ToString(),
                        Text = $"{u.FirstName} {u.LastName}"
                    }).ToList();
                ViewData["CopyID"] = _context.Copies
                    .Include(c => c.Book)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CopyID.ToString(),
                        Text = $"{c.Book.Title} ({c.CatalogNumber})"
                    }).ToList();
                return View(loan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania wypożyczenia do edycji, ID {LoanID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania wypożyczenia do edycji.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Loan loan)
        {
            if (id != loan.LoanID)
            {
                _logger.LogWarning("Niezgodność ID wypożyczenia {LoanID} w edycji", id);
                return NotFound();
            }

            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation("Raw form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}: {kv.Value}")));
            _logger.LogInformation("Otrzymano UserID: {UserID}, CopyID: {CopyID}, DueDate: {DueDate}, ReturnDate: {ReturnDate}",
                loan.UserID, loan.CopyID, loan.DueDate.ToString("o"), loan.ReturnDate?.ToString("o"));

            try
            {
                // Validate inputs
                if (loan.UserID <= 0)
                {
                    ModelState.AddModelError("UserID", "Proszę wybrać użytkownika.");
                }
                if (loan.CopyID <= 0)
                {
                    ModelState.AddModelError("CopyID", "Proszę wybrać egzemplarz.");
                }
                if (loan.DueDate == default || loan.DueDate.Year < 1753)
                {
                    ModelState.AddModelError("DueDate", "Data zwrotu jest nieprawidłowa (musi być po 1753 roku).");
                }
                if (loan.ReturnDate.HasValue && loan.ReturnDate.Value.Year < 1753)
                {
                    ModelState.AddModelError("ReturnDate", "Faktyczna data zwrotu jest nieprawidłowa (musi być po 1753 roku).");
                }

                var newCopy = await _context.Copies.FindAsync(loan.CopyID);
                if (newCopy == null)
                {
                    ModelState.AddModelError("CopyID", "Wybrany egzemplarz nie istnieje.");
                }

                var user = await _context.Users.FindAsync(loan.UserID);
                if (user == null)
                {
                    ModelState.AddModelError("UserID", "Wybrany użytkownik nie istnieje.");
                }

                // Remove navigation property validation errors
                ModelState.Remove("User");
                ModelState.Remove("Copy");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    _logger.LogWarning("ModelState errors: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
                    ViewData["UserID"] = _context.Users
                        .Select(u => new SelectListItem
                        {
                            Value = u.UserID.ToString(),
                            Text = $"{u.FirstName} {u.LastName}"
                        }).ToList();
                    ViewData["CopyID"] = _context.Copies
                        .Include(c => c.Book)
                        .Select(c => new SelectListItem
                        {
                            Value = c.CopyID.ToString(),
                            Text = $"{c.Book.Title} ({c.CatalogNumber})"
                        }).ToList();
                    return View(loan);
                }

                // Load original loan
                var originalLoan = await _context.Loans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LoanID == id);
                if (originalLoan == null)
                {
                    _logger.LogWarning("Wypożyczenie o ID {LoanID} nie zostało znalezione", id);
                    return NotFound();
                }

                // Update Copy.Available logic
                if (originalLoan.CopyID != loan.CopyID && !loan.ReturnDate.HasValue)
                {
                    var oldCopy = await _context.Copies.FindAsync(originalLoan.CopyID);
                    if (oldCopy != null)
                    {
                        oldCopy.Available = true;
                        _context.Update(oldCopy);
                    }
                    newCopy.Available = false;
                    _context.Update(newCopy);
                }
                else if (loan.ReturnDate.HasValue && !originalLoan.ReturnDate.HasValue)
                {
                    newCopy.Available = true;
                    _context.Update(newCopy);
                }
                else if (!loan.ReturnDate.HasValue && originalLoan.ReturnDate.HasValue)
                {
                    newCopy.Available = false;
                    _context.Update(newCopy);
                }

                // Update existing loan
                var existingLoan = await _context.Loans.FindAsync(id);
                if (existingLoan == null)
                {
                    return NotFound();
                }

                existingLoan.UserID = loan.UserID;
                existingLoan.CopyID = loan.CopyID;
                existingLoan.DueDate = loan.DueDate;
                existingLoan.ReturnDate = loan.ReturnDate;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Wypożyczenie {LoanID} zostało zaktualizowane", loan.LoanID);
                TempData["Success"] = "Wypożyczenie zostało zaktualizowane.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji wypożyczenia {LoanID}. Inner exception: {InnerException}",
                    loan.LoanID, ex.InnerException?.Message);
                TempData["Error"] = "Wystąpił błąd podczas aktualizacji wypożyczenia: " + (ex.InnerException?.Message ?? ex.Message);
                ViewData["UserID"] = _context.Users
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserID.ToString(),
                        Text = $"{u.FirstName} {u.LastName}"
                    }).ToList();
                ViewData["CopyID"] = _context.Copies
                    .Include(c => c.Book)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CopyID.ToString(),
                        Text = $"{c.Book.Title} ({c.CatalogNumber})"
                    }).ToList();
                return View(loan);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Usuwanie wypożyczenia wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var loan = await _context.Loans
                    .Include(l => l.User)
                    .Include(l => l.Copy)
                    .ThenInclude(c => c.Book)
                    .FirstOrDefaultAsync(l => l.LoanID == id);
                if (loan == null)
                {
                    _logger.LogWarning("Wypożyczenie o ID {LoanID} nie zostało znalezione", id);
                    return NotFound();
                }
                return View(loan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania wypożyczenia do usunięcia, ID {LoanID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania wypożyczenia do usunięcia.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var loan = await _context.Loans
                    .Include(l => l.Copy)
                    .FirstOrDefaultAsync(l => l.LoanID == id);
                if (loan == null)
                {
                    _logger.LogWarning("Wypożyczenie o ID {LoanID} nie zostało znalezione", id);
                    return NotFound();
                }

                if (loan.ReturnDate == null)
                {
                    var copy = await _context.Copies.FindAsync(loan.CopyID);
                    if (copy != null)
                    {
                        copy.Available = true;
                        _context.Update(copy);
                    }
                }

                _context.Loans.Remove(loan);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Wypożyczenie o ID {LoanID} zostało usunięte", id);
                TempData["Success"] = "Wypożyczenie zostało usunięte.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania wypożyczenia o ID {LoanID}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania wypożyczenia.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Szczegóły wypożyczenia wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var loan = await _context.Loans
                    .Include(l => l.User)
                    .Include(l => l.Copy)
                    .ThenInclude(c => c.Book)
                    .FirstOrDefaultAsync(l => l.LoanID == id);
                if (loan == null)
                {
                    _logger.LogWarning("Wypożyczenie o ID {LoanID} nie zostało znalezione", id);
                    return NotFound();
                }
                return View(loan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów wypożyczenia dla ID {LoanID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania szczegółów wypożyczenia.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}