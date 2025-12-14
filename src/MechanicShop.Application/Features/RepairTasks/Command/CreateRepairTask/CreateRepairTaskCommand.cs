using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.RepairTasks.Command.CreateRepairTask
{
    public sealed record CreateRepairTaskCommand(
       string? Name,
       decimal LaborCost,
       RepairDurationInMinutes? EstimatedDurationInMins,
       List<CreateRepairTaskPartCommand> Parts
   ) : IRequest<Result<RepairTaskDto>>;





}
