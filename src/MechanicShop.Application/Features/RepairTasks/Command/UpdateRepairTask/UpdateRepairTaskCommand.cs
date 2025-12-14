using AutoMapper;
using MechanicShop.Application.Features.Customers.Command.CreateCustomer;
using MechanicShop.Application.Features.RepairTasks.Command.CreateRepairTask;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.RepairTasks.Command.UpdateRepairTask
{

    public sealed record UpdateRepairTaskCommand( Guid RepairTaskId,string Name,
        decimal LaborCost,RepairDurationInMinutes EstimatedDurationInMins,
        List<UpdateRepairTaskPartCommand> Parts) : IRequest<Result<Updated>>;



}
