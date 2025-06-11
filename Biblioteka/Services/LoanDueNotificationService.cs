using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Biblioteka.Data;
using Biblioteka.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Biblioteka.Services
{
    public class LoanDueNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LoanDueNotificationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check daily

        public LoanDueNotificationService(IServiceProvider serviceProvider, ILogger<LoanDueNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LoanDueNotificationService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotificationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for due loans.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("LoanDueNotificationService stopped.");
        }

        private async Task CheckAndSendNotificationsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();

            var now = DateTime.Now;
            var twoDaysFromNow = now.AddDays(2).Date.AddDays(1).AddTicks(-1); // End of the day in 2 days

            var loansDueSoon = await context.Loans
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .Include(l => l.User)
                .Where(l => l.ReturnDate == null && l.DueDate <= twoDaysFromNow && l.DueDate >= now)
                .ToListAsync(stoppingToken);

            foreach (var loan in loansDueSoon)
            {
                // Check if a notification for this loan was already sent in the last 24 hours
                var recentNotification = await context.Notifications
                    .Where(n => n.UserID == loan.UserID &&
                                n.Message.Contains($"Książka '{loan.Copy.Book.Title}'") &&
                                n.SentDate >= now.AddHours(-24))
                    .AnyAsync(stoppingToken);

                if (!recentNotification)
                {
                    var notification = new Notification
                    {
                        UserID = loan.UserID,
                        Message = $"Przypomnienie: Książka '{loan.Copy.Book.Title}' powinna zostać zwrócona do {loan.DueDate:dd-MM-yyyy}.",
                        SentDate = now
                    };

                    context.Notifications.Add(notification);
                    _logger.LogInformation("Created notification for user {UserID} regarding loan {LoanID}.", loan.UserID, loan.LoanID);
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}