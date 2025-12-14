using MechanicShop.Application.Common.BaseDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.Customers.Dtos
{
    public class CustomerDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<VehicleDto> Vehicles { get; set; } = [];
    }

}
