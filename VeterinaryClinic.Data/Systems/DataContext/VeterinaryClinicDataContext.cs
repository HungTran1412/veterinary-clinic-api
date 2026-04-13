using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Data
{
    public partial class VeterinaryClinicDataContext : DbContext
    {
        private readonly IContextAccessor _contextAccessor;

        public VeterinaryClinicDataContext(DbContextOptions<VeterinaryClinicDataContext> options, Func<IContextAccessor> contextAccessorFactory) : base(options)
        {
            _contextAccessor = contextAccessorFactory?.Invoke();
        }

        public virtual DbSet<Permission> Permissions { get; set; }
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<VcDoctorSpecializations>()
                .HasKey(ds => new { ds.DoctorId, ds.SpecializationId });
        }
        
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.Entity is TrackedChangeEntity entity)
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
            return base.SaveChangesAsync(cancellationToken);
        }
    }   
}