using AutoMapper;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Labors.Dtos;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Employees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.Labors.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Employee, LaborDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FullName));

            
        }
        
    }

}
