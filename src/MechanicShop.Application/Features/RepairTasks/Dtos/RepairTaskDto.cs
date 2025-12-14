using MechanicShop.Application.Common.BaseDto;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Domain.RepairTasks.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.RepairTasks.Dtos
{
    public class RepairTaskDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public RepairDurationInMinutes EstimatedDurationInMins { get; set; }
        public decimal LaborCost { get; set; }
        public decimal TotalCost { get; set; }
        public List<PartDto> Parts { get; set; } = [];
    }


}
