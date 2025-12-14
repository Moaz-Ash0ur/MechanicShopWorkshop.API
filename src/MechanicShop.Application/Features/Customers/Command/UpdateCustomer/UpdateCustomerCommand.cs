using AutoMapper;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Customers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.Customers.Command.UpdateCustomer
{
    public sealed record UpdateCustomerCommand(Guid CustomerId,string Name,string PhoneNumber, string Email,
        List<UpdateVehicleCommand> Vehicles) : IRequest<Result<Updated>>;
}
