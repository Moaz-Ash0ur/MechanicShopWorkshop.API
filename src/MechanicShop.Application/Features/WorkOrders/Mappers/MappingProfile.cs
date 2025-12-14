using AutoMapper;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Labors.Dtos;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Parts;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.WorkOrders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.WorkOrders.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<WorkOrder, WorkOrderDto>()

                .ForMember(d => d.TotalPartsCost,
                o => o.MapFrom(s =>
                    s.RepairTasks
                        .SelectMany(t => t.Parts)
                        .Sum(p => p.Cost * p.Quantity)))

            .ForMember(d => d.TotalLaborCost,
                o => o.MapFrom(s =>
                    s.RepairTasks.Sum(t => t.LaborCost)))

            .ForMember(d => d.Total,
                o => o.MapFrom(s =>
                    s.RepairTasks.Sum(t => t.TotalCost)))

            .ForMember(d => d.TotalDurationInMins,
                o => o.MapFrom(s =>
                    s.RepairTasks.Sum(t => (int)t.EstimatedDurationInMins)));

            

            CreateMap<WorkOrder, WorkOrderListItemDto>();



            //CreateMap<RepairTask, RepairTaskDto>();
            //CreateMap<Part, PartDto>();



            //6c3e1a5b-3a69-4ee7-b992-f0bfd2978092 completed

        }


    }

}
