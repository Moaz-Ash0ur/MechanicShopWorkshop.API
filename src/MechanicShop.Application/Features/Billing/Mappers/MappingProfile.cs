using AutoMapper;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Workorders.Billing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechanicShop.Application.Features.Billings.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<InvoiceLineItem, InvoiceLineItemDto>();


            CreateMap<Invoice, InvoiceDto>()
                .ForMember(d => d.Items,
                o => o.MapFrom(s => s.LineItems))
;

            
            
        }
        
    }
}

