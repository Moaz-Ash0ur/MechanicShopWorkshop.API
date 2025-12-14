using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Domain.Workorders.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.RelocateWorkOrder;

public sealed record RelocateWorkOrderCommand(Guid WorkOrderId, DateTimeOffset NewStartAt,
    Spot NewSpot) : IRequest<Result<Updated>>;




public class RelocateWorkOrderCommandHandler : IRequestHandler<RelocateWorkOrderCommand, Result<Updated>>
{
    private readonly ILogger<RelocateWorkOrderCommandHandler> _logger;
    private readonly IAppDbContext _context;
    private readonly HybridCache _cache;
    private readonly IWorkOrderPolicy _workOrderPolicy;


    public RelocateWorkOrderCommandHandler(ILogger<RelocateWorkOrderCommandHandler> logger, IAppDbContext context,
        HybridCache cache, IWorkOrderPolicy workOrderPolicy)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _workOrderPolicy = workOrderPolicy;
    }

    public async Task<Result<Updated>> Handle(RelocateWorkOrderCommand command, CancellationToken ct)
    {

        var workOrder = await _context.WorkOrders
           //.Include(a => a.RepairTasks)
           //.Include(a => a.Labor)
           //.Include(a => a.Vehicle)
           .FirstOrDefaultAsync(a => a.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            _logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);
            return ApplicationErrors.WorkOrderNotFound;
        }


        var duration = workOrder.EndAtUtc.Subtract(workOrder.StartAtUtc).Duration();

        var endAt = command.NewStartAt.Add(duration);


        var checkSpotAvailabilityResult = await _workOrderPolicy.CheckSpotAvailabilityAsync(command.NewSpot,command.NewStartAt,
           endAt, command.WorkOrderId,ct);


        if (checkSpotAvailabilityResult.IsError)
        {
            _logger.LogError("Spot: {Spot} is not available.", workOrder.Spot.ToString());

            return checkSpotAvailabilityResult.Errors;
        }

        if (await _workOrderPolicy.IsLaborOccupied(workOrder.LaborId, command.WorkOrderId, command.NewStartAt, endAt))
        {
            _logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", workOrder.LaborId);

            return ApplicationErrors.LaborOccupied;
        }

        if (await _workOrderPolicy.IsVehicleAlreadyScheduled(workOrder.VehicleId, command.NewStartAt, endAt, command.WorkOrderId))
        {
            _logger.LogError("Vehicle with Id '{VehicleId}' already has an overlapping WorkOrder.", workOrder.VehicleId);

            return ApplicationErrors.VehicleSchedulingConflict;
        }


        var updateTimingResult = workOrder.UpdateTiming(command.NewStartAt, endAt);

        if (updateTimingResult.IsError)
        {
            _logger.LogError("Failed to update timing: {Error}", updateTimingResult.TopError.Description);

            return updateTimingResult.Errors;
        }


        var updateSpotResult = workOrder.UpdateSpot(command.NewSpot);

        if (updateTimingResult.IsError)
        {
            _logger.LogError("Failed to update Spot: {Error}", updateSpotResult.TopError.Description);

            return updateTimingResult.Errors;
        }

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _context.SaveChangesAsync(ct);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _cache.RemoveByTagAsync("work-order", ct);

        return Result.Updated;

    }









}
