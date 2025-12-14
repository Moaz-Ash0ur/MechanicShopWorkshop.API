using AutoMapper;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Events;
using MechanicShop.Domain.WorkOrders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.CreateWorkOrder;

public class CreateWorkOrderCommandHandler: IRequestHandler<CreateWorkOrderCommand, Result<WorkOrderDto>>
{
    private readonly ILogger<CreateWorkOrderCommandHandler> _logger;
    private readonly IAppDbContext _context;
    private readonly HybridCache _cache;
    private readonly IWorkOrderPolicy _workOrderPolicy;
    private readonly IMapper _mapper;

    public CreateWorkOrderCommandHandler(ILogger<CreateWorkOrderCommandHandler> logger,IAppDbContext context,
        HybridCache cache,IWorkOrderPolicy workOrderValidator,IMapper mapper)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _workOrderPolicy = workOrderValidator;
        _mapper = mapper;
    }

    public async Task<Result<WorkOrderDto>> Handle(CreateWorkOrderCommand command, CancellationToken ct)
    {
        var repairTasks = await _context.RepairTasks
            .Where(t => command.RepairTaskIds.Contains(t.Id))
            .ToListAsync(ct);


        //checl if ids repair task sent not found in mechaniShop
        if(repairTasks.Count != command.RepairTaskIds.Count)
        {
            var missingIds = command.RepairTaskIds.Except(repairTasks.Select(rt => rt.Id)).ToArray();

            _logger.LogError("Some RepairTaskIds not found: {MissingIds}", string.Join(", ", missingIds));

            return ApplicationErrors.RepairTaskNotFound;
        }

        //calc th duration depand repair tasl info forr expected time
        var totalEstimatedDuration = TimeSpan.FromMinutes(repairTasks.Sum(r => (int)r.EstimatedDurationInMins));
        var endAt = command.StartAt.Add(totalEstimatedDuration);

        //check if the time customer choice is valid or not
        if (_workOrderPolicy.IsOutsideOperatingHours(command.StartAt, totalEstimatedDuration))
        {
            _logger.LogError("The WorkOrder time ({StartAt} ? {EndAt}) is outside of store operating hours.", command.StartAt, endAt);

            return ApplicationErrors.WorkOrderOutsideOperatingHour(command.StartAt, endAt);
        }


        var checkMinRequirementResult = _workOrderPolicy.ValidateMinimumRequirement(command.StartAt, endAt);

        if (checkMinRequirementResult.IsError)
        {
            _logger.LogError("WorkOrder duration is shorter than the configured minimum.");

            return checkMinRequirementResult.Errors;
        }

        //check if the spot vilable in this time
        var checkSpotAvailabilityResult = await _workOrderPolicy.CheckSpotAvailabilityAsync(command.Spot,command.StartAt,
         endAt,excludeWorkOrderId : null,ct);

        if (checkSpotAvailabilityResult.IsError)
        {
            _logger.LogError("Spot: {Spot} is not available.", command.Spot.ToString());
            return checkSpotAvailabilityResult.Errors;
        }

        //check if vehicle for this work order is found
        var vehicle = await _context.Vehicles
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.Id == command.VehicleId, cancellationToken: ct);

        if (vehicle is null)
        {
            _logger.LogError("Vehicle with Id '{VehicleId}' does not exist.", command.VehicleId);

            return ApplicationErrors.VehicleNotFound;
        }

        var labor = await _context.Employees.FindAsync(command.LaborId, ct);

        if (labor is null)
        {
            _logger.LogError("Invalid LaborId: {LaborId}", command.LaborId.ToString());
            return ApplicationErrors.LaborNotFound;
        }

        //check if the work order create have same wiht the same Vehicle and time
        var hasVehicleConflict = await _context.WorkOrders
            .AnyAsync(w => w.VehicleId == command.VehicleId &&
                w.StartAtUtc.Date == command.StartAt.Date && w.StartAtUtc < endAt &&
                w.EndAtUtc > command.StartAt,ct);


        if (hasVehicleConflict)
        {
            _logger.LogError("Vehicle with Id '{VehicleId}' already has an overlapping WorkOrder.", command.VehicleId);
            return Error.Conflict(
                code: "Vehicle_Overlapping_WorkOrders",
                description: "The vehicle already has an overlapping WorkOrder.");
        }


        var isLaborOccupied = await _context.WorkOrders
            .AnyAsync(w => w.LaborId == command.LaborId
            && w.StartAtUtc < endAt && w.EndAtUtc > command.StartAt, ct);


        if (isLaborOccupied)
        {
            _logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", command.LaborId);
            return Error.Conflict(
                code: "Labor_Occupied",
                description: "Labor is already occupied during the requested time.");
        }

        var createWorkOrderResult = WorkOrder.Create(Guid.NewGuid(),command.VehicleId,command.StartAt,endAt,
            command.LaborId!.Value,command.Spot,repairTasks);

        if (createWorkOrderResult.IsError)
        {
            _logger.LogError("Failed to create WorkOrder: {Error}", createWorkOrderResult.TopError.Description);

            return createWorkOrderResult.Errors;
        }

        var workOrder = createWorkOrderResult.Value;

        _context.WorkOrders.Add(workOrder);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _context.SaveChangesAsync(ct);

        workOrder.Vehicle = vehicle;
        workOrder.Labor = labor;

        _logger.LogInformation("WorkOrder with Id '{WorkOrderId}' created successfully.", workOrder.Id);

        await _cache.RemoveByTagAsync("work-order", ct);

        return _mapper.Map<WorkOrderDto>(workOrder);
       
    }
}





