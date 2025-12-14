namespace MechanicShop.Application.Features.RepairTasks.Command.UpdateRepairTask
{
    public sealed record UpdateRepairTaskPartCommand(Guid? PartId,string Name,decimal Cost,int Quantity);



}
