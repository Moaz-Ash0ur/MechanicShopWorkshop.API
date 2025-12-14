using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Domain.Workorders.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.UpdateOrderState;

public class UpdateWorkOrderStateCommandHandler : IRequestHandler<UpdateWorkOrderStateCommand,Result<Updated>>
{

    private readonly ILogger<UpdateWorkOrderStateCommandHandler> _logger;
    private readonly IAppDbContext _context;
    private readonly HybridCache _cache;
    private readonly IWorkOrderPolicy _workOrderPolicy;
    private readonly TimeProvider _dateTime;

    public UpdateWorkOrderStateCommandHandler(ILogger<UpdateWorkOrderStateCommandHandler> logger, IAppDbContext context,
        HybridCache cache, IWorkOrderPolicy workOrderValidator, TimeProvider dateTime)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _workOrderPolicy = workOrderValidator;
        _dateTime = dateTime;
    }

    public async Task<Result<Updated>> Handle(UpdateWorkOrderStateCommand command, CancellationToken ct)
    {

        var workOrder = await _context.WorkOrders
            .FirstOrDefaultAsync(a => a.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            _logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);

            return ApplicationErrors.WorkOrderNotFound;
        }

        if (workOrder.StartAtUtc > _dateTime.GetUtcNow())
        {
            _logger.LogError("State transition for WorkOrder Id '{WorkOrderId}` is not allowed before the work order�s scheduled start time.", command.WorkOrderId);

            return WorkOrderErrors.StateTransitionNotAllowed(workOrder.StartAtUtc);
        }

        var updateStatusResult = workOrder.UpdateState(command.State);

        if (updateStatusResult.IsError)
        {
            _logger.LogError("Failed to update status: {Error}", updateStatusResult.TopError.Description);

            return updateStatusResult.Errors;
        }

        if (command.State == WorkOrderState.Completed)
        {
            workOrder.AddDomainEvent(new WorkOrderCompleted { WorkOrderId = command.WorkOrderId });
        }

        await _context.SaveChangesAsync(ct);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _cache.RemoveByTagAsync("work-order", ct);

        return Result.Updated;
    }



}

