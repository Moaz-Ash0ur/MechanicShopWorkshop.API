using AutoMapper;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Labors.Dtos;
using MechanicShop.Application.Features.Labors.Mappers;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Application.Features.Scheduling.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Scheduling.Queries.GetDailyScheduleQuery;

public class GetDailyScheduleQueryHandler : IRequestHandler<GetDailyScheduleQuery, Result<ScheduleDto>>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _datetime;
    private readonly IMapper _mapper;

    public GetDailyScheduleQueryHandler(IAppDbContext context,TimeProvider datetime, IMapper mapper)
    {
        _context = context;
        _datetime = datetime;
        _mapper = mapper;
    }

    public async Task<Result<ScheduleDto>> Handle(GetDailyScheduleQuery query, CancellationToken ct)
    {
        var localStart = query.ScheduleDate.ToDateTime(TimeOnly.MinValue);//->(3/12/2025)
        var localEnd = localStart.AddDays(1);//->(4/12/2025)

        //convert start and end date to utc to check wiht work order time beacuse it store at UTC
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, query.TimeZone);// 5 local | 3 utc (3/12/2025)
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, query.TimeZone);// 5 local | 3 utc (4/12/2025)

        var workOrders = await _context.WorkOrders
            .AsNoTracking()
            .Where(w =>
                w.StartAtUtc < utcEnd &&
                w.EndAtUtc > utcStart && (query.LaborId == null || w.LaborId == query.LaborId))
            .Include(w => w.RepairTasks)
            .Include(w => w.Vehicle)
            .Include(w => w.Labor)
            .ToListAsync(ct);


        //calc the current time in customer
        var now = TimeZoneInfo.ConvertTime(_datetime.GetUtcNow(), query.TimeZone); //6:00 pm

        //ready general schedule
        var result = new ScheduleDto
        {
            OnDate = query.ScheduleDate,
            EndOfDay = localEnd < now,
            Spots = []
        };

        //built slot time for each spot (A,B,C,D)
        foreach (var spot in Enum.GetValues<Spot>())
        {
            var current = localStart;
            var slots = new List<AvailabilitySlotDto>();

            //get by spot and order depand time start
            var woBySpot = workOrders
                .Where(w => w.Spot == spot)
                .OrderBy(w => w.StartAtUtc)
                .ToList();//first spot A


            while (current < localEnd)
            {
                //for each 15 min in day make slot
                var next = current.AddMinutes(15);

                //convert start and end to utc for each slot spot
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(current, query.TimeZone);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(next, query.TimeZone);

                //check if any work order found in this time
                var wo = woBySpot.FirstOrDefault(w =>
                    w.StartAtUtc < endUtc && w.EndAtUtc > startUtc);

                if (wo != null)
                {
                    if (!slots.Any(s => s.WorkOrderId == wo.Id))
                    {
                        slots.Add(new AvailabilitySlotDto
                        {
                            WorkOrderId = wo.Id,
                            Spot = spot,
                            StartAt = wo.StartAtUtc,
                            EndAt = wo.EndAtUtc,
                            Vehicle = FormatVehicleInfo(wo.Vehicle!),
                            Labor = _mapper.Map<LaborDto>(wo.Labor!),
                            IsOccupied = true,
                            RepairTasks = [.. wo.RepairTasks.ToList().ConvertAll(rt => _mapper.Map<RepairTaskDto>(rt))],
                            WorkOrderLocked = !wo.IsEditable,
                            State = wo.State,
                            IsAvailable = false
                        });
                    }
                }
                else
                {
                    slots.Add(new AvailabilitySlotDto
                    {
                        Spot = spot,
                        StartAt = startUtc,
                        EndAt = endUtc,
                        WorkOrderLocked = false,
                        IsAvailable = current >= now //13:30 > 13:20 allow os in future
                    });
                }

                current = next;
            }

            result.Spots.Add(new SpotDto
            {
                Spot = spot,
                Slots = slots
            });
        }

        return result;
    }

    private static string? FormatVehicleInfo(Vehicle vehicle) =>
        vehicle != null ? $"{vehicle.Make} | {vehicle.LicensePlate}" : null;
}