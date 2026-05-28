using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Business.Services;
using VeterinaryClinic.Business.Models;

namespace VeterinaryClinic.Business
{
    public class CreateUserCommand : IRequest<Unit>
    {
        public CreateUserModel Model { get; }

        public CreateUserCommand(CreateUserModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateUserCommand, Unit>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly ICacheService _cacheService;
            private readonly IStringLocalizer<CreateUserCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;
            private readonly IBcryptPasswordHasher _passwordHasher;
            private readonly IMediator _mediator;
            private readonly INotificationService _notificationService;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                ICacheService cacheService,
                IStringLocalizer<CreateUserCommand> localizer,
                Func<IContextAccessor> contextAccessorFactory,
                IBcryptPasswordHasher passwordHasher,
                IMediator mediator,
                INotificationService notificationService)
            {
                _dataContext = dataContext;
                _cacheService = cacheService;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
                _passwordHasher = passwordHasher;
                _mediator = mediator;
                _notificationService = notificationService;
            }

            public async Task<Unit> Handle(CreateUserCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                Log.Information($"Create User: " + JsonSerializer.Serialize(model));

                #region Validate

                // validate username
                if (!ValidationUtils.IsValidUsername(model.UserName))
                {
                    throw new ArgumentException($"{_localizer["user.invalid.username"]}");
                }

                //validate email
                if (!ValidationUtils.IsValidEmail(model.Email))
                {
                    throw new ArgumentException($"{_localizer["user.invalid.email_format"]}");
                }

                //validate password
                if (!ValidationUtils.IsValidPassword(model.Password))
                {
                    throw new ArgumentException($"{_localizer["user.invalid.password_complexity"]}");
                }

                var currentUserRole = _contextAccessor.Role;
                var upperRole = model.Role.Trim().ToUpper();

                if (currentUserRole == Role.RECEPTIONIST.ToString())
                {
                    // Receptionist can only create customers
                    upperRole = Role.CUSTOMER.ToString();
                }
                else if (currentUserRole == Role.ADMIN.ToString())
                {
                    // Admin can create DOCTOR or RECEPTIONIST
                    if (upperRole != Role.DOCTOR.ToString() && upperRole != Role.RECEPTIONIST.ToString())
                    {
                        throw new ArgumentException($"Admin can only create users with roles: {Role.DOCTOR}, {Role.RECEPTIONIST}");
                    }
                }
                else
                {
                    throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                }
                
                // Validate that specializations are only provided for doctors
                if (upperRole != Role.DOCTOR.ToString() && model.SpecializationIds != null && model.SpecializationIds.Any())
                {
                    throw new ArgumentException(_localizer["user.specialization.not_for_receptionist"]);
                }

                model = model with { Role = upperRole };

                #endregion

                var entity = AutoMapperUtils.AutoMap<CreateUserModel, VcUsers>(model);

                if (entity == null)
                {
                    throw new ArgumentException("Failed to map data.");
                }

                entity.Address = string.IsNullOrEmpty(entity.Address) ? "" : entity.Address;
                entity.AvatarUrl = string.IsNullOrEmpty(entity.AvatarUrl) ? "" : entity.AvatarUrl;
                entity.CreatedUserId = _contextAccessor.UserId;

                #region Check Duplicate
                var checkCode = await _dataContext.VcUsers.AnyAsync(x => x.Code == entity.Code, cancellationToken);
                if (checkCode)
                {
                    throw new ArgumentException($"{_localizer["user.existed.code"]}");
                }

                var checkUsername = await _dataContext.VcUsers.AnyAsync(x => x.Username == entity.Username, cancellationToken);
                if (checkUsername)
                {
                    throw new ArgumentException($"{_localizer["user.existed.username"]}");
                }

                var checkEmail = await _dataContext.VcUsers.AnyAsync(x => x.Email == entity.Email, cancellationToken);
                if (checkEmail)
                {
                    throw new ArgumentException($"{_localizer["user.existed.email"]}");
                }

                var checkPhoneNumber = await _dataContext.VcUsers.AnyAsync(x => x.PhoneNumber == entity.PhoneNumber, cancellationToken);
                if (checkPhoneNumber)
                {
                    throw new ArgumentException($"{_localizer["user.existed.phone-number"]}");
                }
                #endregion

                string password = model.Password;
                entity.Password = _passwordHasher.HashPassword(password);

                await _dataContext.VcUsers.AddAsync(entity, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                // If the user is a DOCTOR and has specializations, add them.
                if (entity.Role == Role.DOCTOR.ToString() && model.SpecializationIds != null && model.SpecializationIds.Any())
                {
                    var specializationModel = new DoctorSpecializationModel
                    {
                        DoctorId = entity.Id,
                        SpecializationIds = model.SpecializationIds
                    };
                    await _mediator.Send(new CreateDoctorSpecializationCommand(specializationModel), cancellationToken);
                }

                // Gửi email sử dụng Hangfire
                string subject = "Thông báo cấp tài khoản - Phòng khám thú y";
                string body = EmailTemplates.GetAccountCreatedEmail(
                    entity.FullName,
                    entity.Username,
                    password,
                    entity.Role
                );

                BackgroundJob.Enqueue<IEmailService>(emailService => emailService.SendEmailAsync(entity.Email, subject, body));

                // Gửi thông báo real-time
                var notification = new NotificationModel
                {
                    UserId = entity.Id,
                    Title = subject,
                    Message = $"Chào mừng {entity.FullName}! Tài khoản của bạn đã được tạo thành công.",
                    Type = NotificationType.MESSAGE.ToString(),
                    RelatedEntityId = entity.Id,
                    RelatedEntityType = RelatedEntityType.User.ToString()
                };
                await _notificationService.SendAndSaveNotificationAsync(notification);

                Log.Information($"Account created email job enqueued for {entity.Email}");

                _cacheService.Remove(UserConstant.BuildCacheKey());

                return Unit.Value;
            }
        }
    }
}
