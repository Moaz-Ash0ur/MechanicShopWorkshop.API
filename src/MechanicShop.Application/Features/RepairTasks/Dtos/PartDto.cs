using MechanicShop.Application.Common.BaseDto;

namespace MechanicShop.Application.Features.RepairTasks.Dtos
{
    public class PartDto : BaseDto
    {
        public string? Name { get;  set; }
        public decimal Cost { get;  set; }
        public int Quantity { get;  set; }
    }


}
