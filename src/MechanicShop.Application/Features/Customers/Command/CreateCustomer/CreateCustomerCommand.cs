using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Domain.Common.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.Customers.Command.CreateCustomer
{
    public sealed record CreateCustomerCommand(string Name,string PhoneNumber,string Email,
     List<CreateVehicleCommand> Vehicles) : IRequest<Result<CustomerDto>>;






}
