using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DelEdAllotment.Services
{
    public class EmailBatchService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailBatchService> _logger;

        public EmailBatchService(IServiceProvider serviceProvider, ILogger<EmailBatchService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var controller = scope.ServiceProvider.GetRequiredService<DelEdAllotment.Controllers.EmailController>();
                    bool keepSending = true;

                    while (keepSending && !stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var result = await controller.SendAdmitCardEmails();
                            if (result is ObjectResult objectResult)
                            {
                                var value = objectResult.Value?.ToString() ?? "";

                                // ✅ Stop if no more emails left
                                if (value.Contains("All emails have already been sent"))
                                {
                                    _logger.LogInformation("✅ All emails sent. Stopping batch loop.");
                                    keepSending = false;
                                    break;
                                }
                            }

                            _logger.LogInformation($"✅ Batch processed at {DateTime.Now}");
                            // No delay - send next batch immediately
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"❌ Error in auto email sending: {ex.Message}");
                            keepSending = false;
                        }
                    }
                }

                // ⏳ Wait before checking again (maybe next day or restart manually)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
