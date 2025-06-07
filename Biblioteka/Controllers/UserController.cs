using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Biblioteka.Data;
using Biblioteka.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Biblioteka.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UsersController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(LibraryContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy użytkowników");
                TempData["Error"] = "Wystąpił błąd podczas pobierania listy użytkowników.";
                return View(new System.Collections.Generic.List<User>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
                return View(user);
            }

            try
            {
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == user.Email.ToLower()))
                {
                    ModelState.AddModelError("Email", "Email jest już w użyciu.");
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Użytkownik {Email} został utworzony", user.Email);
                TempData["Success"] = "Użytkownik został dodany.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia użytkownika {Email}", user.Email);
                ModelState.AddModelError("Email", "Email jest już w użyciu.");
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia użytkownika {Email}", user.Email);
                TempData["Error"] = "Wystąpił błąd podczas dodawania użytkownika.";
                return View(user);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edycja użytkownika wywołana z pustym ID");
                return NotFound();
            }

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Użytkownik o ID {UserID} nie został znaleziony", id);
                    return NotFound();
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania użytkownika do edycji, ID {UserID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania użytkownika do edycji.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.UserID)
            {
                _logger.LogWarning("Niezgodność ID użytkownika {UserID} w edycji", id);
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
                return View(user);
            }

            try
            {
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == user.Email.ToLower() && u.UserID != id))
                {
                    ModelState.AddModelError("Email", "Email jest już w użyciu.");
                    return View(user);
                }

                _context.Update(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Użytkownik {Email} został zaktualizowany", user.Email);
                TempData["Success"] = "Użytkownik został zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji użytkownika {Email}", user.Email);
                ModelState.AddModelError("Email", "Email jest już w użyciu.");
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji użytkownika {Email}", user.Email);
                TempData["Error"] = "Wystąpił błąd podczas aktualizacji użytkownika.";
                return View(user);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Usuwanie użytkownika wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(m => m.UserID == id);
                if (user == null)
                {
                    _logger.LogWarning("Użytkownik o ID {UserID} nie został znaleziony", id);
                    return NotFound();
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania użytkownika do usunięcia, ID {UserID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania użytkownika do usunięcia.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Użytkownik o ID {UserID} nie został znaleziony", id);
                    return NotFound();
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Użytkownik o ID {UserID} został usunięty", id);
                TempData["Success"] = "Użytkownik został usunięty.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
            {
                _logger.LogError(ex, "Nie można usunąć użytkownika o ID {UserID} z powodu powiązanych danych", id);
                TempData["Error"] = "Nie można usunąć użytkownika, ponieważ jest powiązany z innymi danymi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania użytkownika o ID {UserID}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania użytkownika.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Szczegóły użytkownika wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserID == id);
                if (user == null)
                {
                    _logger.LogWarning("Użytkownik o ID {UserID} nie został znaleziony", id);
                    return NotFound();
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów użytkownika dla ID {UserID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania szczegółów użytkownika.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}