using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Biblioteka.Data;
using Biblioteka.Models;
using System.Collections.Generic;

namespace Biblioteka.Controllers
{
    [Route("api")]
    [ApiController]
    public class GenericApiController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GenericApiController> _logger;
        private readonly LibraryContext _context;

        public GenericApiController(IServiceProvider serviceProvider, ILogger<GenericApiController> logger, LibraryContext context)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _context = context;
        }

        [HttpGet("{controllerName}")]
        [Authorize]
        public async Task<IActionResult> Get(string controllerName, [FromQuery] Dictionary<string, string> queryParams)
        {
            try
            {
                // Find the controller by name
                var controllerType = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .FirstOrDefault(t => t.IsSubclassOf(typeof(ControllerBase)) &&
                                        t.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase));

                if (controllerType == null)
                {
                    _logger.LogWarning("Controller not found: {ControllerName}", controllerName);
                    return NotFound(new { Error = $"Kontroler {controllerName} nie istnieje." });
                }

                // Check authorization
                var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();
                if (authorizeAttribute != null)
                {
                    var roles = authorizeAttribute.Roles?.Split(',') ?? Array.Empty<string>();
                    if (roles.Any() && !roles.Any(role => User.IsInRole(role)))
                    {
                        _logger.LogWarning("User lacks permission for controller {ControllerName}", controllerName);
                        return Forbid();
                    }
                }

                // Create controller instance
                var controller = ActivatorUtilities.CreateInstance(_serviceProvider, controllerType) as ControllerBase;
                if (controller == null)
                {
                    _logger.LogError("Failed to create instance of controller {ControllerName}", controllerName);
                    return StatusCode(500, new { Error = "Błąd podczas tworzenia kontrolera." });
                }

                // Find Index action
                var methodInfo = controllerType.GetMethod("Index");
                if (methodInfo == null)
                {
                    _logger.LogWarning("Controller {ControllerName} does not have an Index action", controllerName);
                    return NotFound(new { Error = $"Kontroler {controllerName} nie ma akcji Index." });
                }

                // Prepare parameters for Index action
                object result;
                if (controllerName.Equals("Books", StringComparison.OrdinalIgnoreCase))
                {
                    var searchModel = new BookSearchViewModel
                    {
                        Title = queryParams.GetValueOrDefault("Title"),
                        Author = queryParams.GetValueOrDefault("Author"),
                        ISBN = queryParams.GetValueOrDefault("ISBN"),
                        YearFrom = queryParams.TryGetValue("YearFrom", out var yearFrom) && int.TryParse(yearFrom, out var yf) ? yf : null,
                        YearTo = queryParams.TryGetValue("YearTo", out var yearTo) && int.TryParse(yearTo, out var yt) ? yt : null,
                        CategoryIds = queryParams.ContainsKey("CategoryIds") ? queryParams["CategoryIds"].Split(',').Select(int.Parse).ToList() : null
                    };
                    result = await (Task<IActionResult>)methodInfo.Invoke(controller, new object[] { searchModel });
                }
                else
                {
                    result = await (Task<IActionResult>)methodInfo.Invoke(controller, null);
                }

                // Process result
                if (result is ViewResult viewResult)
                {
                    var data = viewResult.Model;
                    var structuredData = FormatResult(data, controllerName);
                    _logger.LogInformation("Retrieved data from controller {ControllerName}", controllerName);
                    return Ok(structuredData);
                }

                _logger.LogWarning("Index action in controller {ControllerName} did not return a ViewResult", controllerName);
                return StatusCode(500, new { Error = "Nieprawidłowy typ wyniku z akcji Index." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data from controller {ControllerName}", controllerName);
                return StatusCode(500, new { Error = "Wystąpił błąd podczas pobierania danych." });
            }
        }

        private object FormatResult(object model, string controllerName)
        {
            switch (controllerName.ToLower())
            {
                case "books":
                    var books = model as IEnumerable<Book>;
                    if (books == null) return new List<object>();
                    return books.Select(b => new
                    {
                        b.BookID,
                        b.Title,
                        b.Author,
                        b.ISBN,
                        b.YearPublished,
                        Categories = b.BookCategories != null
                            ? b.BookCategories.Select(bc => (object)new { bc.CategoryID, bc.Category.Name }).ToList()
                            : new List<object>()
                    }).ToList();

                case "notifications":
                    var notifications = model as IEnumerable<Notification>;
                    if (notifications == null) return new List<object>();
                    return notifications.Select(n => new
                    {
                        n.NotificationID,
                        User = n.User != null ? $"{n.User.FirstName} {n.User.LastName}" : "Brak użytkownika",
                        n.Message,
                        SentDate = n.SentDate.ToString("yyyy-MM-dd")
                    }).ToList();

                case "users":
                    var users = model as IEnumerable<User>;
                    if (users == null) return new List<object>();
                    return users.Select(u => new
                    {
                        u.UserID,
                        u.FirstName,
                        u.LastName,
                        u.Email
                    }).ToList();

                case "loans":
                    var loans = model as IEnumerable<Loan>;
                    if (loans == null) return new List<object>();
                    return loans.Select(l => new
                    {
                        l.LoanID,
                        User = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "Brak użytkownika",
                        BookTitle = l.Copy?.Book?.Title ?? "Brak książki",
                        l.LoanDate,
                        l.DueDate,
                        ReturnDate = l.ReturnDate?.ToString("yyyy-MM-dd")
                    }).ToList();

                case "copies":
                    var copies = model as IEnumerable<Copy>;
                    if (copies == null) return new List<object>();
                    return copies.Select(c => new
                    {
                        c.CopyID,
                        BookTitle = c.Book?.Title ?? "Brak książki"
                    }).ToList();

                case "categories":
                    var categories = model as IEnumerable<Category>;
                    if (categories == null) return new List<object>();
                    return categories.Select(c => new
                    {
                        c.CategoryID,
                        c.Name
                    }).ToList();

                default:
                    return model ?? new List<object>();
            }
        }
    }
}