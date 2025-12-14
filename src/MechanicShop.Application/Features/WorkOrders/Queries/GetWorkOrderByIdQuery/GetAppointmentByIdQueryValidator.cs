using FluentValidation;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery
{
    public sealed class GetAppointmentByIdQueryValidator : AbstractValidator<GetWorkOrderByIdQuery>
    {
        public GetAppointmentByIdQueryValidator()
        {
            RuleFor(request => request.Id)
                .NotEmpty()
                .WithErrorCode("WorkOrderId_Is_Required")
                .WithMessage("WorkOrderId is required.");
        }
    }

    //-------------------------------Get All--------------------------







}
