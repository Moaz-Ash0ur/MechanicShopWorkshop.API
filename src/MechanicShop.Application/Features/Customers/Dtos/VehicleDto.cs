using MechanicShop.Application.Common.BaseDto;

namespace MechanicShop.Application.Features.Customers.Dtos
{
    public class  VehicleDto : BaseDto
    {
        public  string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string LicensePlate {  get; set; }
    }


  
}
