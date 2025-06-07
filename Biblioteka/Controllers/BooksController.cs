using Biblioteka.Data;
using Biblioteka.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Biblioteka.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ILogger<BooksController> _logger;

        public BooksController(LibraryContext context, ILogger<BooksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var books = await _context.Books
                    .Include(b => b.BookCategories)
                    .ThenInclude(bc => bc.Category)
                    .ToListAsync();
                return View(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving books list");
                TempData["Error"] = "Wystąpił błąd podczas pobierania listy książek.";
                return View(new List<Book>());
            }
        }

        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories for book creation");
                TempData["Error"] = "Wystąpił błąd podczas przygotowywania formularza.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Create(Book book, List<int> CategoryIds)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(book);
                    await _context.SaveChangesAsync();

                    if (CategoryIds != null && CategoryIds.Any())
                    {
                        foreach (var categoryId in CategoryIds)
                        {
                            _context.BookCategories.Add(new BookCategory { BookID = book.BookID, CategoryID = categoryId });
                        }
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Book {BookTitle} created successfully", book.Title);
                    TempData["Success"] = "Książka została dodana.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating book {BookTitle}", book.Title);
                    TempData["Error"] = "Wystąpił błąd podczas dodawania książki.";
                }
            }
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(book);
        }

        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit book called with null ID");
                return NotFound();
            }

            try
            {
                var book = await _context.Books
                    .Include(b => b.BookCategories)
                    .ThenInclude(bc => bc.Category)
                    .FirstOrDefaultAsync(b => b.BookID == id);
                if (book == null)
                {
                    _logger.LogWarning("Book with ID {BookId} not found", id);
                    return NotFound();
                }

                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.SelectedCategoryIds = book.BookCategories.Select(bc => bc.CategoryID).ToList();
                return View(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book for edit, ID {BookId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania książki do edycji.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Edit(int id, Book book, List<int> CategoryIds)
        {
            if (id != book.BookID)
            {
                _logger.LogWarning("Mismatched book ID {BookId} in edit", id);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBook = await _context.Books
                        .Include(b => b.BookCategories)
                        .FirstOrDefaultAsync(b => b.BookID == id);
                    if (existingBook == null)
                    {
                        _logger.LogWarning("Book with ID {BookId} not found", id);
                        return NotFound();
                    }

                    _context.Entry(existingBook).CurrentValues.SetValues(book);

                    existingBook.BookCategories.Clear();
                    if (CategoryIds != null && CategoryIds.Any())
                    {
                        foreach (var categoryId in CategoryIds)
                        {
                            existingBook.BookCategories.Add(new BookCategory { BookID = book.BookID, CategoryID = categoryId });
                        }
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Book {BookTitle} updated successfully", book.Title);
                    TempData["Success"] = "Książka została zaktualizowana.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating book {BookTitle}", book.Title);
                    TempData["Error"] = "Wystąpił błąd podczas aktualizacji książki.";
                }
            }
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategoryIds = CategoryIds ?? new List<int>();
            return View(book);
        }

        [Authorize(Roles = "Bibliotekarz,Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete book called with null ID");
                return NotFound();
            }

            try
            {
                var book = await _context.Books
                    .Include(b => b.BookCategories)
                    .ThenInclude(bc => bc.Category)
                    .FirstOrDefaultAsync(b => b.BookID == id);
                if (book == null)
                {
                    _logger.LogWarning("Book with ID {BookId} not found", id);
                    return NotFound();
                }
                return View(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book for delete, ID {BookId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania książki do usunięcia.";
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
                var book = await _context.Books.FindAsync(id);
                if (book == null)
                {
                    _logger.LogWarning("Book with ID {BookId} not found", id);
                    return NotFound();
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Book with ID {BookId} deleted successfully", id);
                TempData["Success"] = "Książka została usunięta.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ID {BookId}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania książki.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details book called with null ID");
                return NotFound();
            }

            try
            {
                var book = await _context.Books
                    .Include(b => b.BookCategories)
                    .ThenInclude(bc => bc.Category)
                    .FirstOrDefaultAsync(b => b.BookID == id);
                if (book == null)
                {
                    _logger.LogWarning("Book with ID {BookId} not found", id);
                    return NotFound();
                }
                return View(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book details for ID {BookId}", id);
                TempData["Error"] = "Wystąpił błąd podczas pobierania szczegółów książki.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}