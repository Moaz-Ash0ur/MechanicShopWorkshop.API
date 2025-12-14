using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Common.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery
{
    public sealed record GetWorkOrderByIdQuery(Guid Id) : IRequest<Result<WorkOrderDto>>;






}
