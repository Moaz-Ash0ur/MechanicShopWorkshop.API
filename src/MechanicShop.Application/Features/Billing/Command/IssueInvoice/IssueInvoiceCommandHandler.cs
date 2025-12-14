using AutoMapper;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Constants;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Billing.Command.IssueInvoice
{
    public class IssueInvoiceCommandHandler : IRequestHandler<IssueInvoiceCommand, Result<InvoiceDto>>
    {
        private readonly ILogger<IssueInvoiceCommandHandler> _logger;
        private readonly IAppDbContext _context;
        private readonly HybridCache _cache;
        private readonly TimeProvider _datetime;
        private readonly IMapper _mapper;


        public IssueInvoiceCommandHandler(ILogger<IssueInvoiceCommandHandler> logger, IAppDbContext context, HybridCache cache, TimeProvider datetime, IMapper mapper)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
            _datetime = datetime;
            _mapper = mapper;
        }


        public async Task<Result<InvoiceDto>> Handle(IssueInvoiceCommand cmd, CancellationToken ct)
        {

            var workOrder = await _context.WorkOrders.AsNoTracking()
                .Include(w => w.Vehicle)
                      .ThenInclude(v => v!.Customer)
                .Include(w => w.RepairTasks)
                     .ThenInclude(rt => rt.Parts)
                .FirstOrDefaultAsync(w => w.Id == cmd.WorkOrderId , ct);

            if (workOrder is null)
            {
                _logger.LogWarning("Invoice issuance failed. WorkOrder {WorkOrderId} not found.", cmd.WorkOrderId);

                return ApplicationErrors.WorkOrderNotFound;
            }


            if (workOrder.State != WorkOrderState.Completed)
            {
                _logger.LogWarning("Invoice issuance rejected. WorkOrder {WorkOrderId} is not in completed.", cmd.WorkOrderId);

                return ApplicationErrors.WorkOrderMustBeCompletedForInvoicing;
            }


            Guid invoiceId = Guid.NewGuid();

            var lineItems = new List<InvoiceLineItem>();

            var lineNumber = 1;


            foreach (var (task , taskIndex) in workOrder.RepairTasks.Select((t,i) => (t, i + 1)))
            {
                var partSummary = task.Parts.Any() ? string.Join(Environment.NewLine, 
                    task.Parts.Select(p => $"{p.Name} x{p.Quantity} @ {p.Cost:C}")) :" • No parts";


                var lineDescription = $"{taskIndex}:{task.Name}{Environment.NewLine}" +
                    $" Labor = {task.LaborCost:C} {Environment.NewLine}" +
                    $" Part  =  {Environment.NewLine}{partSummary}";


                var totalPartCost = task.Parts.Sum(p => p.Quantity  * p.Cost);
                var totalTask = totalPartCost  + task.LaborCost;


               var lineItemResult = InvoiceLineItem.Create(invoiceId, lineNumber++, lineDescription, 1 ,totalTask);

                if (lineItemResult.IsError)
                {
                    return lineItemResult.Errors;
                }

                lineItems.Add(lineItemResult.Value);
            }

            var subtotal = lineItems.Sum(li => li.LineTotal);
            var taxAmount = subtotal * MechanicShopConstants.TaxRate;

            var discountAmount = workOrder.Discount ?? 0m;

            var createInvoiceResult = Invoice.Create(invoiceId,workOrder.Id, lineItems, discountAmount, taxAmount, _datetime);


            if (createInvoiceResult.IsError)
            {
                _logger.LogWarning(
                     "Invoice creation failed for WorkOrderId: {WorkOrderId}. Errors: {@Errors}",
                     cmd.WorkOrderId,
                     createInvoiceResult.Errors);

                return createInvoiceResult.Errors;
            }

            var invoice = createInvoiceResult.Value;

            await _context.Invoices.AddAsync(invoice, ct);

            await _context.SaveChangesAsync(ct);

            await _cache.RemoveByTagAsync("invoice", ct);

            _logger.LogInformation("Invoice {InvoiceId} issued for WorkOrder {WorkOrderId}.", invoice.Id, workOrder.Id);

            return _mapper.Map<InvoiceDto>(invoice);
        }



    }




}
