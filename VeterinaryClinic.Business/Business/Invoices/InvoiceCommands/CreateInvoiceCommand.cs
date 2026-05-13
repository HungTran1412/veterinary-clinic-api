using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business;

public class CreateInvoiceCommand : IRequest<Unit>
{
    public CreateInvoiceModel Model { get; }

    /// <summary>
    /// Them moi hoa don
    /// </summary>
    public CreateInvoiceCommand(CreateInvoiceModel model)
    {
        Model = model; 
    }

    public class Handler : IRequestHandler<CreateInvoiceCommand, Unit>
    {
        private readonly VeterinaryClinicDataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IStringLocalizer<CreateInvoiceCommand> _localizer;
        private readonly IContextAccessor _contextAccessor;

        public Handler(VeterinaryClinicDataContext dataContext, ICacheService cacheService, IStringLocalizer<CreateInvoiceCommand> localizer, Func<IContextAccessor> contextAccessorFactory)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _localizer = localizer;
            _contextAccessor = contextAccessorFactory();
        }

        public async Task<Unit> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
        {
            var model = request.Model;
            Log.Information($"Create Invoice: " + JsonSerializer.Serialize(model));
            
            // Check if Appointment exists
            var checkAppointment = await _dataContext.VcAppointments.AnyAsync(x => x.Id == model.AppointmentId && x.IsActive, cancellationToken);
            if (!checkAppointment)
            {
                throw new ArgumentException(_localizer["appointment.not_found"]);
            }

            //map du lieu
            var entity = AutoMapperUtils.AutoMap<CreateInvoiceModel, VcInvoices>(model);

            if (entity == null)
            {
                throw new ArgumentException(_localizer["data.not_found"]);
            }

            entity.Code = GenerateCodeUtils.GenerateUserCode("INV");
            var checkCode = await _dataContext.VcInvoices.AnyAsync(x => x.Code == entity.Code, cancellationToken);
            if (checkCode)
            {
                throw new ArgumentException($"{_localizer["invoice.existed.code"]}");
            }
            
            //luu db
            await _dataContext.VcInvoices.AddAsync(entity, cancellationToken);
            await _dataContext.SaveChangesAsync(cancellationToken);
            
            //xoa cache
            _cacheService.Remove(InvoiceConstant.BuildCacheKey());
            
            return Unit.Value;
        }
    }
}