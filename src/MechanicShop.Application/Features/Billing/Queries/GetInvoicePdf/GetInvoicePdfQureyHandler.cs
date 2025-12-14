using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Billing.Queries.GetInvoicePdf;

public class GetInvoicePdfQureyHandler : IRequestHandler<GetInvoicePdfQuery, Result<InvoicePdfDto>>
{
    private readonly ILogger<GetInvoicePdfQureyHandler> _logger;
    private readonly IInvoicePdfGenerator _pdfGenerator;
    private readonly IAppDbContext _context;

    public GetInvoicePdfQureyHandler(ILogger<GetInvoicePdfQureyHandler> logger,
    IInvoicePdfGenerator pdfGenerator,IAppDbContext context
    )
    {
        _logger = logger;
        _pdfGenerator = pdfGenerator;
        _context = context;
    }

    public async Task<Result<InvoicePdfDto>> Handle(GetInvoicePdfQuery query, CancellationToken ct)
    {
        var invoice = await _context.Invoices.AsNoTracking()
           .Include(i => i.LineItems)
           .FirstOrDefaultAsync(i => i.Id == query.InvoiceId, ct);

        if (invoice is null)
        {
            _logger.LogWarning("Invoice not found. InvoiceId: {InvoiceId}", query.InvoiceId);
            return Error.NotFound("Invoice not found.");
        }

        try
        {
            var pdfBytes = _pdfGenerator.Generate(invoice);

            var invoicePdf = new InvoicePdfDto
            {
                Content = pdfBytes,
                FileName = $"invoice-{invoice.Id}.pdf"
            };

            return invoicePdf;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for InvoiceId: {InvoiceId}", query.InvoiceId);
            return Error.Failure("An error occurred while generating the invoice PDF.");
        }
    }
}