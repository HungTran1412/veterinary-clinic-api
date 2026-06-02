using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business;

public class CreatePaymentCommand : IRequest<Unit>
{
    public CreatePaymentModel Model { get; }

    /// <summary>
    /// Them moi thanh toan
    /// </summary>
    public CreatePaymentCommand(CreatePaymentModel model)
    {
        Model = model; 
    }

    public class Handler : IRequestHandler<CreatePaymentCommand, Unit>
    {
        private readonly VeterinaryClinicDataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IStringLocalizer<CreatePaymentCommand> _localizer;
        private readonly IContextAccessor _contextAccessor;

        public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<CreatePaymentCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _localizer = localizer;
            _contextAccessor = contextAccessorFactory();
        }

        public async Task<Unit> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            var model = request.Model;
            Log.Information($"Create Payment: " + JsonSerializer.Serialize(model));
            
            // Check if Invoice exists
            var checkInvoice = await _dataContext.VcInvoices.AnyAsync(x => x.Id == model.InvoiceId && x.IsActive, cancellationToken);
            if (!checkInvoice)
            {
                throw new ArgumentException(_localizer["invoice.not_found"]);
            }

            //map du lieu
            var entity = AutoMapperUtils.AutoMap<CreatePaymentModel, VcPayments>(model);

            if (entity == null)
            {
                throw new ArgumentException(_localizer["data.not_found"]);
            }

            entity.Code = GenerateCodeUtils.GenerateCodeByDaily("PAY");
            var checkCode = await _dataContext.VcPayments.AnyAsync(x => x.Code == entity.Code, cancellationToken);
            if (checkCode)
            {
                throw new ArgumentException($"{_localizer["Payment.existed.code;"]}");
            }
            
            //luu db
            await _dataContext.VcPayments.AddAsync(entity, cancellationToken);
            await _dataContext.SaveChangesAsync(cancellationToken);
            
            //xoa cache
            _cacheService.Remove(PaymentConstant.BuildCacheKey());
            
            return Unit.Value;
        }
    }
}