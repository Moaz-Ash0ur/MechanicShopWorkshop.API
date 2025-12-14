using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Infrastructure.BackgroundJobs
{
    public class OverdueBookingCleanupService : BackgroundService
    {

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverdueBookingCleanupService> _logger;
        private readonly TimeProvider _dateTime;
        private readonly AppSettings _appSettings;

        public OverdueBookingCleanupService(IServiceScopeFactory scopeFactory, ILogger<OverdueBookingCleanupService> logger, TimeProvider dateTime, IOptions<AppSettings> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _dateTime = dateTime;
            _appSettings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //each x time do this job as backgroundServices
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_appSettings.OverdueBookingCleanupFrequencyMinutes));
           

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Checking overdue work orders at {Now}", _dateTime.GetUtcNow());

                //write logic inside try catch
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                    var cutoff = _dateTime.GetUtcNow().AddMinutes(-_appSettings.BookingCancellationThresholdMinutes);

                    var workOrderFound = await db.WorkOrders
                        .Where(w => w.State == WorkOrderState.Scheduled 
                        && w.StartAtUtc <= cutoff)
                        .ToListAsync(stoppingToken);

                    if(workOrderFound.Count > 0)
                    {
                        foreach (var wo in workOrderFound) {

                            var result = wo.Cancel();

                            if (result.IsError)
                            {
                                _logger.LogWarning("Failed to cancel WorkOrder {Id}: {Error}", wo.Id, result.Errors);
                            }

                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("Cancelled {Count} overdue work orders: {Ids}", workOrderFound.Count, workOrderFound.Select(w => w.Id));
                        }

                    }
                    else
                    {
                        _logger.LogInformation("No overdue work orders found.");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up overdue work orders.");
                }

            }


        }



    }

}
