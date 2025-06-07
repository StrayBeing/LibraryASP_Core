using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Biblioteka.Data;
using Biblioteka.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Text.Json;
using System.Collections.Generic;

namespace Biblioteka.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(LibraryContext context, ILogger<NotificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var notifications = await _context.Notifications
                    .Include(n => n.User)
                    .ToListAsync();
                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy powiadomień");
                TempData["Error"] = "Wystąpił błąd podczas pobierania listy powiadomień.";
                return View(new List<Notification>());
            }
        }

        public async Task<IActionResult> MyNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Nie można uzyskać ID użytkownika dla Klienta");
                    TempData["Error"] = "Błąd podczas pobierania danych użytkownika.";
                    return View(new List<Notification>());
                }

                var notifications = await _context.Notifications
                    .Include(n => n.User)
                    .Where(n => n.UserID == userId)
                    .ToListAsync();
                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania powiadomień dla użytkownika");
                TempData["Error"] = "Wystąpił błąd podczas pobierania powiadomień.";
                return View(new List<Notification>());
            }
        }

        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public IActionResult Create()
        {
            try
            {
                ViewData["UserID"] = new SelectList(
                    _context.Users.Select(u => new
                    {
                        Value = u.UserID,
                        Text = $"{u.FirstName} {u.LastName}"
                    }),
                    "Value",
                    "Text"
                );
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas przygotowywania formularza tworzenia powiadomienia");
                TempData["Error"] = "Wystąpił błąd podczas przygotowania formularza.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Create(Notification notification)
        {
            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation("FormData: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}: {kv.Value}")));
            _logger.LogInformation("Otrzymano UserID: {UserID}, Message: {Message}", notification.UserID, notification.Message);

            try
            {
                ModelState.Remove("User");

                if (notification.UserID <= 0)
                {
                    ModelState.AddModelError("UserID", "Proszę wybrać użytkownika.");
                }
                if (string.IsNullOrWhiteSpace(notification.Message))
                {
                    ModelState.AddModelError("Message", "Wiadomość jest wymagana.");
                }

                var user = await _context.Users.FindAsync(notification.UserID);
                if (user == null)
                {
                    ModelState.AddModelError("UserID", "Wybrany użytkownik nie istnieje.");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    );
                    _logger.LogWarning("Errors: {Errors}", JsonSerializer.Serialize(errors));
                    ViewData["UserID"] = new SelectList(
                        _context.Users.Select(u => new
                        {
                            Value = u.UserID,
                            Text = $"{u.FirstName} {u.LastName}"
                        }),
                        "Value",
                        "Text"
                    );
                    return View(notification);
                }

                notification.SentDate = DateTime.Now;
                _context.Add(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Powiadomienie {NotificationID} zostało utworzone", notification.NotificationID);
                TempData["Success"] = "Powiadomienie zostało dodane.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia powiadomienia");
                TempData["Error"] = "Wystąpił błąd podczas dodania powiadomienia: " + ex.Message;
                ViewData["UserID"] = new SelectList(
                    _context.Users.Select(u => new
                    {
                        Value = u.UserID,
                        Text = $"{u.FirstName} {u.LastName}"
                    }),
                    "Value",
                    "Text"
                );
                return View(notification);
            }
        }

        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Edycja powiadomienia wywołana z pustym lub niepoprawnym ID {NotificationID}", id);
                return NotFound();
            }

            try
            {
                var notification = await _context.Notifications
                    .Include(n => n.User)
                    .FirstOrDefaultAsync(n => n.NotificationID == id);
                if (notification == null)
                {
                    _logger.LogWarning("Powiadomienie o ID {NotificationID} nie zostało znalezione", id);
                    return NotFound();
                }

                ViewData["UserID"] = new SelectList(
                    _context.Users.Select(u => new
                    {
                        Value = u.UserID,
                        Text = $"{u.FirstName} {u.LastName}"
                    }),
                    "Value",
                    "Text",
                    notification.UserID
                );
                return View(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania powiadomienia do edycji, ID {NotificationID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania powiadomienia do edycji.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Edit(int id, Notification notification)
        {
            if (id != notification.NotificationID)
            {
                _logger.LogError("Niezgodność ID powiadomienia {NotificationID} w edycji", id);
                return NotFound();
            }

            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation("FormData: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}: {kv.Value}")));
            _logger.LogInformation("Otrzymano UserID: {UserID}, Message: {Message}, SentDate: {SentDate}",
                notification.UserID, notification.Message, notification.SentDate.ToString("o"));

            try
            {
                ModelState.Remove("User");

                if (notification.UserID <= 0)
                {
                    ModelState.AddModelError("UserID", "Proszę wybrać użytkownika.");
                }
                if (string.IsNullOrWhiteSpace(notification.Message))
                {
                    ModelState.AddModelError("Message", "Wiadomość jest wymagana.");
                }
                if (notification.SentDate == default || notification.SentDate.Year < 1753)
                {
                    ModelState.AddModelError("SentDate", "Data wysyłania jest nieprawidłowa (musi być po 1753 roku).");
                }

                var user = await _context.Users.FindAsync(notification.UserID);
                if (user == null)
                {
                    ModelState.AddModelError("UserID", "Wybrany użytkownik nie istnieje.");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    );
                    _logger.LogWarning("Errors: {Errors}", JsonSerializer.Serialize(errors));
                    ViewData["UserID"] = new SelectList(
                        _context.Users.Select(u => new
                        {
                            Value = u.UserID,
                            Text = $"{u.FirstName} {u.LastName}"
                        }),
                        "Value",
                        "Text",
                        notification.UserID
                    );
                    return View(notification);
                }

                var existingNotification = await _context.Notifications.FindAsync(id);
                if (existingNotification == null)
                {
                    _logger.LogWarning("Powiadomienie o ID {NotificationID} nie zostało znalezione", id);
                    return NotFound();
                }

                existingNotification.UserID = notification.UserID;
                existingNotification.Message = notification.Message;
                existingNotification.SentDate = notification.SentDate;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Powiadomienie {NotificationID} zostało zaktualizowane", notification.NotificationID);
                TempData["Success"] = "Powiadomienie zostało zaktualizowane.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji powiadomienia {NotificationID}. Inner exception: {InnerException}", id, ex.InnerException?.Message);
                TempData["Error"] = "Wystąpił błąd podczas aktualizacji powiadomienia: " + (ex.InnerException?.Message ?? ex.Message);
                ViewData["UserID"] = new SelectList(
                    _context.Users.Select(u => new
                    {
                        Value = u.UserID,
                        Text = $"{u.FirstName} {u.LastName}"
                    }),
                    "Value",
                    "Text",
                    notification.UserID
                );
                return View(notification);
            }
        }

        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Usuwanie powiadomienia wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var notification = await _context.Notifications
                    .Include(n => n.User)
                    .FirstOrDefaultAsync(n => n.NotificationID == id);
                if (notification == null)
                {
                    _logger.LogWarning("Powiadomienie o ID {NotificationID} nie zostało znalezione", id);
                    return NotFound();
                }
                return View(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania powiadomienia do usunięcia, ID {NotificationID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania powiadomienia do usunięcia.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    _logger.LogWarning("Powiadomienie o ID {NotificationID} nie zostało znalezione", id);
                    return NotFound();
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Powiadomienie o ID {NotificationID} zostało usunięte", id);
                TempData["Success"] = "Powiadomienie zostało usunięte.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania powiadomienia o ID {NotificationID}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania powiadomienia.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Szczegóły powiadomienia wywołane z pustym ID");
                return NotFound();
            }

            try
            {
                var notification = await _context.Notifications
                    .Include(n => n.User)
                    .FirstOrDefaultAsync(n => n.NotificationID == id);
                if (notification == null)
                {
                    _logger.LogWarning("Powiadomienie o ID {NotificationID} nie zostało znalezione", id);
                    return NotFound();
                }

                // Restrict Klient users to their own notifications
                if (User.IsInRole("Klient"))
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || notification.UserID != userId)
                    {
                        _logger.LogWarning("Klient próbuje uzyskać dostęp do nie swojego powiadomienia ID {NotificationID}", id);
                        return Forbid();
                    }
                }

                return View(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów powiadomienia dla ID {NotificationID}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania szczegółów powiadomienia.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}