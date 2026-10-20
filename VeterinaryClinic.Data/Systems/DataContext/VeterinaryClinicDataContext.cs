using Microsoft.EntityFrameworkCore;
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

        public VeterinaryClinicDataContext(DbContextOptions<VeterinaryClinicDataContext> options, Func<IContextAccessor> contextAccessorFactory,
            IOptions<ReadModelSynchronizationOptions>? synchronizationOptions = null) : base(options)
        {
            _contextAccessor = contextAccessorFactory?.Invoke();
            _captureOutbox = synchronizationOptions?.Value.Enabled ?? false;
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
                });
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
                }, cancellationToken);
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
