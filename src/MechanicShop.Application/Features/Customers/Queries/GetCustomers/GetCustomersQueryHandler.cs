using AutoMapper;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Model;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Customers.Mappers;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Customers;
using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler(IAppDbContext context,IMapper mapper)
    : IRequestHandler<GetCustomersQuery, Result<List<CustomerDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<List<CustomerDto>>> Handle(GetCustomersQuery query, CancellationToken ct)
    {
        var customers = await _context.Customers
            .Include(c => c.Vehicles)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<List<CustomerDto>>(customers);
    }


}