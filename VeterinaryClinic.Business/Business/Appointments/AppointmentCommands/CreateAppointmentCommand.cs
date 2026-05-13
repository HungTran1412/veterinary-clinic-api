using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class CreateAppointmentCommand : IRequest<Unit>
    {
        public CreateAppointmentModel Model { get; }

        public CreateAppointmentCommand(CreateAppointmentModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateAppointmentCommand, Unit>
        {
            private static readonly string[] TimeZoneIds =
            [
                "SE Asia Standard Time",
                "Asia/Ho_Chi_Minh",
                "Asia/Saigon"
            ];

            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<CreateAppointmentCommand> _localizer;
            private readonly ICacheService _cacheService;
            private readonly IAppointmentStateMachine _appointmentStateMachine;
            private readonly MailSettings _mailSettings;
            private readonly IMediator _mediator;
            private readonly IVeterinaryClinicCallStoreHelper _callStoreHelper;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                Func<IContextAccessor> contextAccessorFactory,
                IStringLocalizer<CreateAppointmentCommand> localizer,
                ICacheService cacheService,
                IAppointmentStateMachine appointmentStateMachine,
                IOptions<MailSettings> mailSettings,
                IMediator mediator,
                IVeterinaryClinicCallStoreHelper callStoreHelper
                ) 
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
                _cacheService = cacheService;
                _appointmentStateMachine = appointmentStateMachine;
                _mailSettings = mailSettings.Value;
                _mediator = mediator;
                _callStoreHelper = callStoreHelper;
            }

            public async Task<Unit> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                var currentUserId = _contextAccessor.UserId;
                var currentRole = _contextAccessor.Role;
                Log.Information($"Create Appointment attempt by User {currentUserId}: {JsonSerializer.Serialize(model)}");

                if (model.AppointmentDate.Date < DateTime.UtcNow.Date)
                {
                    throw new ArgumentException(_localizer["appointment.date.in_past"]);
                }

                if (currentRole == Role.DOCTOR.ToString())
                {
                    throw new UnauthorizedAccessException(_localizer["appointment.create.unauthorized"]);
                }

                if (currentRole == Role.CUSTOMER.ToString() && currentUserId != model.CustomerId)
                {
                    throw new UnauthorizedAccessException(_localizer["appointment.create.customer_only_self"]);
                }

                var customer = await _dataContext.VcUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == model.CustomerId && x.Role == Role.CUSTOMER.ToString() && x.IsActive, cancellationToken);
                if (customer == null)
                {
                    throw new ArgumentException(_localizer["appointment.customer.invalid"]);
                }

                var pet = await _dataContext.VcPets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == model.PetId && x.IsActive, cancellationToken);
                if (pet == null)
                {
                    throw new ArgumentException(_localizer["pet.not_found"]);
                }

                if (pet.OwnerId != model.CustomerId)
                {
                    throw new ArgumentException(_localizer["appointment.pet.not_belong_to_customer"]);
                }

                var service = await (from svc in _dataContext.VcServices.AsNoTracking()
                                     join sp in _dataContext.VcSpecializations.AsNoTracking()
                                         on svc.SpecializationId equals sp.Id
                                     where svc.Id == model.SerivceId &&
                                           svc.IsActive &&
                                           svc.IsAvailable &&
                                           sp.IsActive
                                     select new
                                     {
                                         svc.Id,
                                         svc.SpecializationId,
                                         svc.DurationMinutes,
                                         svc.Name ,
                                         svc.Price
                                     })
                    .FirstOrDefaultAsync(cancellationToken);

                if (service == null)
                {
                    throw new ArgumentException(_localizer["appointment.service.invalid"]);
                }

                if (service.DurationMinutes <= 0)
                {
                    throw new ArgumentException(_localizer["appointment.service.duration.invalid"]);
                }

                var appointmentDate = NormalizeToClinicTime(model.AppointmentDate).Date;
                var startTime = appointmentDate.Add(NormalizeToClinicTime(model.StartTime).TimeOfDay);
                var endTime = startTime.AddMinutes(service.DurationMinutes);

                if (startTime >= endTime)
                {
                    throw new ArgumentException(_localizer["appointment.time.invalid"]);
                }

                if (endTime.Date != appointmentDate)
                {
                    throw new ArgumentException(_localizer["appointment.end_time.out_of_day"]);
                }

                // call store find doctor
                var dataTable = _callStoreHelper.CallStoreGetCandidateDoctorsAsync(
                    service.SpecializationId, 
                    appointmentDate, 
                    startTime, 
                    endTime);

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    throw new ArgumentException(_localizer["appointment.doctor.not_available"]);
                }

                var availableDoctors = dataTable.ToList<CandidateDoctorModel>();
                

                var doctorAppointmentLoads = await _dataContext.VcAppointments
                    .AsNoTracking()
                    .Where(x =>
                        availableDoctors.Select(d => d.DoctorId).Contains(x.DoctorId) && // Use Select(d => d.Id)
                        x.AppointmentDate.Date == appointmentDate &&
                        x.State != AppointmentStatus.CANCELLED.ToString() &&
                        x.State != AppointmentStatus.REJECTED.ToString() &&
                        x.State != AppointmentStatus.NO_SHOW.ToString())
                    .GroupBy(x => x.DoctorId)
                    .Select(x => new
                    {
                        DoctorId = x.Key,
                        Count = x.Count()
                    })
                    .ToListAsync(cancellationToken);

                var selectedDoctor = availableDoctors
                    .Select(d => new
                    {
                        d.DoctorId,
                        d.DoctorEmail,
                        d.DoctorName,
                        Count = doctorAppointmentLoads.FirstOrDefault(x => x.DoctorId == d.DoctorId)?.Count ?? 0
                    })
                    .OrderBy(x => x.Count)
                    .ThenBy(x => x.DoctorId)
                    .First();

                var initialStatus = _appointmentStateMachine.GetInitialStatus();
                var entity = new VcAppointments
                {
                    Code = GenerateCodeUtils.GenerateUserCode("APT"),
                    CustomerId = model.CustomerId,
                    PetId = model.PetId,
                    SerivceId = model.SerivceId,
                    DoctorId = selectedDoctor.DoctorId, 
                    AppointmentDate = appointmentDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    CancelReason = string.Empty,
                    Note = model.Note ?? string.Empty,
                    AuthorId = currentUserId?.ToString() ?? string.Empty,
                    ProcessId = null,
                    State = initialStatus.ToString(),
                    StateName = _appointmentStateMachine.GetStateDisplayName(initialStatus),
                    IsFinalState = _appointmentStateMachine.IsFinalStatus(initialStatus),
                };

                //luu vào db
                await _dataContext.VcAppointments.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                
                // tạo hóa đơn mặc định cho appointment
                var invoice = new VcInvoices
                {
                    AppointmentId = entity.Id,
                    Code = GenerateCodeUtils.GenerateUserCode("INV"),
                    Status = PaymentStatus.PENDING.ToString(),
                    TotalAmount = service.Price,
                    PaidDate = default,
                };

                await _dataContext.VcInvoices.AddAsync(invoice, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                //tạo  mới hồ sơ khám bệnh
                using var transaction = await _dataContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    await _dataContext.SaveChangesAsync(cancellationToken);
                    var medicalRecord = new CreateMedicalRecordModel
                    {
                        DoctorId = entity.DoctorId,
                        AppointmentId = entity.Id
                    };
                    await _mediator.Send(new CreateMedicalRecordCommand(medicalRecord));

                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    Console.WriteLine(e);
                    throw;
                }
                
                // Send email notifications using Hangfire
                // Email to Customer
                string customerSubject = "Xác nhận lịch hẹn của bạn - Phòng khám thú y";
                string customerBody = EmailTemplates.GetAppointmentConfirmationEmailForCustomer(
                    customer.FullName,
                    pet.Name,
                    service.Name,
                    entity.AppointmentDate.ToShortDateString(),
                    entity.StartTime.ToShortTimeString(),
                    entity.EndTime.ToShortTimeString(),
                    selectedDoctor.DoctorName, 
                    entity.Code
                );
                BackgroundJob.Enqueue<IEmailService>(emailService => emailService.SendEmailAsync(customer.Email, customerSubject, customerBody));

                // Email to Doctor
                string doctorSubject = "Lịch hẹn mới được tạo - Phòng khám thú y";
                string doctorBody = EmailTemplates.GetAppointmentConfirmationEmailForDoctor(
                    selectedDoctor.DoctorName, 
                    customer.FullName,
                    pet.Name,
                    service.Name,
                    entity.AppointmentDate.ToShortDateString(),
                    entity.StartTime.ToShortTimeString(),
                    entity.EndTime.ToShortTimeString(),
                    entity.Code
                );
                BackgroundJob.Enqueue<IEmailService>(emailService => emailService.SendEmailAsync(selectedDoctor.DoctorEmail, doctorSubject, doctorBody)); 

                Log.Information($"Appointment confirmation email jobs enqueued for customer {customer.Email} and doctor {selectedDoctor.DoctorEmail}.");
                
                // xóa cache
                _cacheService.Remove(AppointmentConstant.BuildCacheKey());
                Log.Information($"Appointment created successfully with Id: {entity.Id}, DoctorId: {entity.DoctorId}");
                
                return Unit.Value;
            }

            private static DateTime NormalizeToClinicTime(DateTime value)
            {
                if (value.Kind == DateTimeKind.Unspecified)
                {
                    return value;
                }

                return TimeZoneInfo.ConvertTime(value, GetClinicTimeZone());
            }

            private static TimeZoneInfo GetClinicTimeZone()
            {
                foreach (var id in TimeZoneIds)
                {
                    try
                    {
                        return TimeZoneInfo.FindSystemTimeZoneById(id);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                    }
                    catch (InvalidTimeZoneException)
                    {
                    }
                }

                return TimeZoneInfo.Local;
            }
        }
    }
}