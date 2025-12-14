using MechanicShop.Domain.Common.Results;
using MediatR;

namespace MechanicShop.Application.Features.RepairTasks.Command.CreateRepairTask
{
    public sealed record CreateRepairTaskPartCommand(
        string Name,
        decimal Cost,
        int Quantity
    ) : IRequest<Result<Success>>;





}
