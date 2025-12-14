using MechanicShop.Application.Common.BaseDto;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Labors.Dtos;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.WorkOrders.Dtos
{
    public class WorkOrderDto : BaseDto
    {
        public Guid? InvoiceId { get; set; }
        public Spot Spot { get; set; }
        public VehicleDto? Vehicle { get; set; }
        public DateTimeOffset StartAtUtc { get; set; }
        public DateTimeOffset EndAtUtc { get; set; }
        public List<RepairTaskDto> RepairTasks { get; set; } = [];
        public LaborDto? Labor { get; set; }
        public WorkOrderState State { get; set; }
        public decimal TotalPartsCost { get; set; }
        public decimal TotalLaborCost { get; set; }
        public decimal Total { get; set; }
        public int TotalDurationInMins { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    //-------------------------------Get All--------------------------








}
