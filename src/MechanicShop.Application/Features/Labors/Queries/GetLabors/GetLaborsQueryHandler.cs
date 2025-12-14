using AutoMapper;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Labors.Dtos;
using MechanicShop.Application.Features.Labors.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Identity;

using MediatR;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace MechanicShop.Application.Features.Labors.Queries.GetLabors;

public class GetLaborsQueryHandler(IAppDbContext context,IMapper mapper)
    : IRequestHandler<GetLaborsQuery, Result<List<LaborDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<List<LaborDto>>> Handle(GetLaborsQuery query, CancellationToken ct)
    {
        var labors = await _context
            .Employees
            .AsNoTracking()
            .Where(e => e.Role == Role.Labor)
            .ToListAsync(ct);

        return _mapper.Map<List<LaborDto>>(labors);
    }
}