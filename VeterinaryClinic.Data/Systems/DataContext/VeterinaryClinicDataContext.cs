using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using VeterinaryClinic.Shared;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Data
{
    public partial class VeterinaryClinicDataContext : DbContext
    {
        private readonly IContextAccessor? _contextAccessor;
        private readonly bool _captureOutbox;
        private readonly ReadModelOutboxTransactionInterceptor? _outboxTransactionInterceptor;
        private readonly Action<DbTransaction>? _outboxCaptured;

        public VeterinaryClinicDataContext(DbContextOptions<VeterinaryClinicDataContext> options, Func<IContextAccessor> contextAccessorFactory,
            IOptions<ReadModelSynchronizationOptions>? synchronizationOptions = null,
            IReadModelSynchronizationSignal? synchronizationSignal = null) : base(options)
        {
            _contextAccessor = contextAccessorFactory?.Invoke();
            _captureOutbox = synchronizationOptions?.Value.Enabled ?? false;
            if (_captureOutbox && synchronizationSignal != null)
            {
                _outboxTransactionInterceptor = new ReadModelOutboxTransactionInterceptor(synchronizationSignal);
                _outboxCaptured = _outboxTransactionInterceptor.MarkCaptured;
            }
        }

        public DbSet<ReadModelOutboxMessage> ReadModelOutboxMessages { get; set; } = null!;
        
        public DbSet<VcPets> VcPets { get; set; }
        public DbSet<VcUsers> VcUsers { get; set; }
        public DbSet<VcInvoices> VcInvoices { get; set; }
        public DbSet<VcServices> VcServices { get; set; }
        public DbSet<VcEmailLogs> VcEmailLogs { get; set; }
        public DbSet<VcAppointments> VcAppointments { get; set; }
        public DbSet<VcNotifications> VcNotifications { get; set; }
        public DbSet<VcWorkSchedules> VcWorkSchedules { get; set; }
        public DbSet<VcMedicalRecords> VcMedicalRecords { get; set; }
        public DbSet<VcSpecializations> VcSpecializations { get; set; }
        public DbSet<VcDoctorSpecializations> VcDoctorSpecializations { get; set; }
        public DbSet<VcUserVerificationTokens> VcUserVerificationTokens { get; set; }
        public DbSet<VcPayments> VcPayments { get; set; }
        public DbSet<VcShiftTemplates> VcShiftTemplates { get; set; }
        public DbSet<VcWorkScheduleRegistrations> VcWorkScheduleRegistrations { get; set; }
        public DbSet<VcBills> VcBills { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (_outboxTransactionInterceptor != null)
                optionsBuilder.AddInterceptors(_outboxTransactionInterceptor);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<VcDoctorSpecializations>()
                .HasKey(ds => new { ds.DoctorId, ds.SpecializationId });

            modelBuilder.Entity<ReadModelOutboxMessage>(entity =>
            {
                entity.ToTable("vcReadModelOutbox");
                entity.HasKey(message => message.Id);
                entity.Property(message => message.Id).ValueGeneratedOnAdd();
                entity.Property(message => message.EntityType).HasMaxLength(512).IsRequired();
                entity.Property(message => message.KeyValuesJson).IsRequired();
                entity.HasIndex(message => new { message.ProcessedAtUtc, message.Id });
            });
        }
        
        public override int SaveChanges() => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            if (_captureOutbox)
            {
                return ReadModelOutboxCapture.SaveChanges(this, acceptAllChangesOnSuccess, accept =>
                {
                    ApplyAudit();
                    return base.SaveChanges(accept);
                }, _outboxCaptured);
            }

            ApplyAudit();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            if (_captureOutbox)
            {
                return ReadModelOutboxCapture.SaveChangesAsync(this, acceptAllChangesOnSuccess, (accept, token) =>
                {
                    ApplyAudit();
                    return base.SaveChangesAsync(accept, token);
                }, cancellationToken, _outboxCaptured);
            }

            ApplyAudit();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ApplyAudit()
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.Entity is TrackedChangeEntity entity && entry.State is EntityState.Added or EntityState.Modified)
                {
                    var userId = _contextAccessor?.UserId ?? 0;
                    var userName = _contextAccessor?.UserName ?? "System";

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entity.CreatedDate = DateTime.Now;
                            entity.CreatedUserId = userId;
                            entity.CreatedUserName = userName;
                            
                            // Gán cả thông tin sửa đổi ban đầu để tránh lỗi NOT NULL trong DB
                            entity.ModifiedDate = DateTime.Now;
                            entity.ModifiedUserId = userId;
                            entity.ModifiedUserName = userName;
                            break;
                        case EntityState.Modified:
                            entity.ModifiedDate = DateTime.Now;
                            entity.ModifiedUserId = userId;
                            entity.ModifiedUserName = userName;
                            break;
                    }
                }
            }
        }
    }   
}
