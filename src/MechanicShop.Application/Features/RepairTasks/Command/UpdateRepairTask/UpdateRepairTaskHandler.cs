using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks.Parts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Command.UpdateRepairTask
{
    public class UpdateRepairTaskHandler : IRequestHandler<UpdateRepairTaskCommand, Result<Updated>>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<UpdateRepairTaskCommand> _logger;
        private readonly HybridCache _cache;

        public UpdateRepairTaskHandler(IAppDbContext context, ILogger<UpdateRepairTaskCommand> logger, HybridCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<Result<Updated>> Handle(UpdateRepairTaskCommand command, CancellationToken ct)
        {
            var repairTask = await _context.RepairTasks
                .Include(rt => rt.Parts)
                .FirstOrDefaultAsync(rt => rt.Id == command.RepairTaskId, ct);

            if (repairTask is null)
            {
                _logger.LogWarning("RepairTask {RepairTaskId} not found for update.", command.RepairTaskId);

                return ApplicationErrors.RepairTaskNotFound;
            }

            var validatedParts = new List<Part>();

            foreach (var p in command.Parts)
            {
                var partId = p.PartId ?? Guid.NewGuid();

                var partResult = Part.Create(partId, p.Name, p.Cost, p.Quantity);

                if (partResult.IsError)
                {
                    return partResult.Errors;
                }

                validatedParts.Add(partResult.Value);
            }

            var updateRepairTaskResult = repairTask.Update(command.Name, command.LaborCost, command.EstimatedDurationInMins);

            if (updateRepairTaskResult.IsError)
            {
                return updateRepairTaskResult.Errors;
            }

            var upsertPartsResult = repairTask.UpsertParts(validatedParts);

            if (upsertPartsResult.IsError)
            {
                return upsertPartsResult.Errors;
            }

            await _context.SaveChangesAsync(ct); // The database operation was expected to affect 1 row(s), but actually affected 0 row(s);

            await _cache.RemoveByTagAsync("repair-task", ct);
            await _cache.RemoveByTagAsync("repair-tasks", ct);

            return Result.Updated;
        }



    }



}
