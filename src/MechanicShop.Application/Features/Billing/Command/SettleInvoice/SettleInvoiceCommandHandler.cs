using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Billing.Commands.SettleInvoice;

public class SettleInvoiceCommandHandler : IRequestHandler<SettleInvoiceCommand, Result<Success>>
{
    private readonly ILogger<SettleInvoiceCommandHandler> _logger;
    private readonly IAppDbContext _context;
    private readonly HybridCache _cache;
    private readonly TimeProvider _datetime;

    public SettleInvoiceCommandHandler(ILogger<SettleInvoiceCommandHandler> logger,IAppDbContext context,
                                   HybridCache cache,TimeProvider datetime)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _datetime = datetime;
    }

    public async Task<Result<Success>> Handle(SettleInvoiceCommand cmd, CancellationToken ct)
    {
        var invoice = await _context.Invoices
             .FirstOrDefaultAsync(w => w.Id == cmd.InvoiceId, ct);

        if (invoice is null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found.", cmd.InvoiceId);
            return ApplicationErrors.InvoiceNotFound;
        }

        var payInvoiceResult = invoice.MarkAsPaid(_datetime);

        if (payInvoiceResult.IsError)
        {
            _logger.LogWarning(
               "Invoice payment failed for InvoiceId: {InvoiceId}. Errors: {Errors}",
               invoice.Id,
               payInvoiceResult.Errors);

            return payInvoiceResult.Errors;
        }

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("invoice", ct);

        _logger.LogInformation("Invoice {InvoiceId} successfully paid.", invoice.Id);

        return Result.Success;
    }



}


