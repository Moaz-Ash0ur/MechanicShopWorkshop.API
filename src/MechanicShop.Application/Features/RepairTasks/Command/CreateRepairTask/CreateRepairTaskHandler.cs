using AutoMapper;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Command.CreateCustomer;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Parts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.RepairTasks.Command.CreateRepairTask
{
    public class CreateRepairTaskHandler : IRequestHandler<CreateRepairTaskCommand, Result<RepairTaskDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<CreateRepairTaskHandler> _logger;
        private readonly HybridCache _cache;
        private readonly IMapper _mapper;

        public CreateRepairTaskHandler(IAppDbContext context, ILogger<CreateRepairTaskHandler> logger, HybridCache cache, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _mapper = mapper;
        }

        public async Task<Result<RepairTaskDto>> Handle(CreateRepairTaskCommand command, CancellationToken ct)
        {
            var nameExists = await _context.RepairTasks
               .AnyAsync(p => EF.Functions.Like(p.Name, command.Name), ct);

            if (nameExists)
            {
                _logger.LogWarning("Duplicate part name '{PartName}'.", command.Name);

                return RepairTaskErrors.DuplicateName;
            }

            List<Part> parts = [];

            foreach (var p in command.Parts)
            {
                var partResult = Part.Create(Guid.NewGuid(), p.Name, p.Cost, p.Quantity);

                if (partResult.IsError)
                {
                    return partResult.Errors;
                }

                parts.Add(partResult.Value);
            }

            var createRepairTaskResult = RepairTask.Create(Guid.NewGuid(),command.Name!,
                        command.LaborCost, command.EstimatedDurationInMins!.Value,parts);

            if (createRepairTaskResult.IsError)
            {
                return createRepairTaskResult.Errors;
            }

            var repairTask = createRepairTaskResult.Value;

            _context.RepairTasks.Add(repairTask);

            await _context.SaveChangesAsync(ct);

            await _cache.RemoveByTagAsync("repair-task", ct);
            await _cache.RemoveByTagAsync("repair-tasks", ct);
           


            return _mapper.Map<RepairTaskDto>(createRepairTaskResult.Value);
        }





    }
}
