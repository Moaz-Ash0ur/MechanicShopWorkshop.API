using AutoMapper;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery
{
    public class GetWorkOrderByIdQueryHandler : IRequestHandler<GetWorkOrderByIdQuery, Result<WorkOrderDto>>
    {
        private readonly ILogger<GetWorkOrderByIdQueryHandler> _logger;
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetWorkOrderByIdQueryHandler(ILogger<GetWorkOrderByIdQueryHandler> logger,IAppDbContext context,IMapper mapper)
        {      
            _logger = logger;
            _context = context;
            _mapper = mapper;          
        }

  
        public async Task<Result<WorkOrderDto>> Handle(GetWorkOrderByIdQuery query, CancellationToken ct)
        {

            var workOrder = await _context.WorkOrders.AsNoTracking()
               .Include(a => a.RepairTasks)
                  .ThenInclude(a => a.Parts)
               .Include(a => a.Labor)
               .Include(a => a.Vehicle!)
                   .ThenInclude(v => v.Customer)
               .Include(a => a.Invoice)
           .FirstOrDefaultAsync(a => a.Id == query.Id, ct);

            if (workOrder is null)
            {
                _logger.LogWarning("WorkOrder with id {WorkOrderId} was not found", query.Id);

                return ApplicationErrors.WorkOrderNotFound;
            }

            return _mapper.Map<WorkOrderDto>(workOrder);
        }

    }

    //-------------------------------Get All--------------------------







}
