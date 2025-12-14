using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Command.AssignLabor
{
    public class AssignLaborCommandHandler : IRequestHandler<AssignLaborCommand, Result<Updated>>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<AssignLaborCommandHandler> _logger;
        private readonly IWorkOrderPolicy _workOrderPolicy;
        private readonly HybridCache _cache;

        public AssignLaborCommandHandler(IAppDbContext context, ILogger<AssignLaborCommandHandler> logger, IWorkOrderPolicy workOrderPolicy, HybridCache cache)
        {
            _context = context;
            _logger = logger;
            _workOrderPolicy = workOrderPolicy;
            _cache = cache;
        }


        public async Task<Result<Updated>> Handle(AssignLaborCommand Command, CancellationToken ct)
        {

            var workOrder = await _context
                .WorkOrders.FirstOrDefaultAsync(w => w.Id == Command.WorkOrderId, ct);

            if (workOrder is null)
            {
                _logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", Command.WorkOrderId);
                return ApplicationErrors.WorkOrderNotFound;
            }


            var labor = await _context.Employees.FindAsync(Command.LaborId,ct);

            if (labor is null)
            {
                _logger.LogError("Invalid LaborId: {LaborId}", Command.LaborId);
                return ApplicationErrors.LaborNotFound;
            }

            if (await _workOrderPolicy.IsLaborOccupied(Command.LaborId, Command.WorkOrderId, workOrder.StartAtUtc, workOrder.EndAtUtc))
            {
                _logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", workOrder.LaborId);
                return ApplicationErrors.LaborOccupied;
            }

              var updateLaborResult = workOrder.UpdateLabor(Command.LaborId);

            if (updateLaborResult.IsError)
            {
                foreach (var error in updateLaborResult.Errors)
                {
                    _logger.LogError("[LaborUpdate] {ErrorCode}: {ErrorDescription}", error.Code, error.Description);
                }

                return updateLaborResult.Errors;
            }

            await _context.SaveChangesAsync(ct);

            await _cache.RemoveByTagAsync("work-order", ct);

            return Result.Updated;
        }




    }









}
