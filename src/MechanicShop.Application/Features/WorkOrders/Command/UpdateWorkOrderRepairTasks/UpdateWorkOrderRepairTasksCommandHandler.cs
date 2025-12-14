using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.Workorders.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;

public class UpdateWorkOrderRepairTasksCommandHandler : IRequestHandler<UpdateWorkOrderRepairTasksCommand, Result<Updated>>
{
    private readonly ILogger<UpdateWorkOrderRepairTasksCommandHandler> _logger;
    private readonly IAppDbContext _context;
    private readonly HybridCache _cache;
    private readonly IWorkOrderPolicy _workOrderPolicy;

    public UpdateWorkOrderRepairTasksCommandHandler(ILogger<UpdateWorkOrderRepairTasksCommandHandler> logger, IAppDbContext context,
        HybridCache cache, IWorkOrderPolicy workOrderPolicy)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _workOrderPolicy = workOrderPolicy;

    }

    public async Task<Result<Updated>> Handle(UpdateWorkOrderRepairTasksCommand command, CancellationToken ct)
    {

       var workOrder = await _context.WorkOrders
            .Include(w => w.RepairTasks)
            .FirstOrDefaultAsync(w => w.Id == command.WorkOrderId,ct);

        if (workOrder is null)
        {
            _logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);
            return ApplicationErrors.WorkOrderNotFound;
        }

        if (command.RepairTaskIds.Length == 0)
        {
            _logger.LogError("Empty RepairTaskIds list submitted.");

            return RepairTaskErrors.AtLeastOneRepairTaskIsRequired;
        }


        var requestedTasks = _context.RepairTasks
            .Where(rp => command.RepairTaskIds.Contains(rp.Id))
            .ToList();

        if (requestedTasks.Count != command.RepairTaskIds.Length)
        {
            var missingIds = command.RepairTaskIds.Except(requestedTasks.Select(t => t.Id)).ToArray();

            _logger.LogError("One or more RepairTasks not found. {ids}", string.Join(", ", missingIds));

            return ApplicationErrors.RepairTaskNotFound;
        }


        var clearExistingResult = workOrder.ClearRepairTasks();

        if (clearExistingResult.IsError)
        {
            return clearExistingResult;
        }


        foreach (var task in requestedTasks)
        {
            var addRepairTaskResult = workOrder.AddRepairTask(task);

            if (addRepairTaskResult.IsError)
            {
                return addRepairTaskResult;
            }
        }


        var totalDuration = TimeSpan.FromMinutes(requestedTasks.Sum(x => (int)x.EstimatedDurationInMins));

        var newEndAt = workOrder.StartAtUtc + totalDuration;

        if (_workOrderPolicy.IsOutsideOperatingHours(workOrder.StartAtUtc, totalDuration))
        {
            return Error.Conflict("WorkOrder_Outside_OperatingHours", "WorkOrder timing exceeds business hours.");
        }

        var spotCheckResult = await _workOrderPolicy.CheckSpotAvailabilityAsync(
            workOrder.Spot,
            workOrder.StartAtUtc,
            newEndAt,
            excludeWorkOrderId: workOrder.Id,
            ct: ct);

        if (spotCheckResult.IsError)
        {
            return spotCheckResult.Errors;
        }

        if (await _workOrderPolicy.IsLaborOccupied(workOrder.LaborId, workOrder.Id, workOrder.StartAtUtc, newEndAt))
        {
            return ApplicationErrors.LaborOccupied;
        }

        workOrder.UpdateTiming(workOrder.StartAtUtc, newEndAt);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _context.SaveChangesAsync(ct);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _cache.RemoveByTagAsync("work-order", ct);
        
        return Result.Updated;

    }


}
