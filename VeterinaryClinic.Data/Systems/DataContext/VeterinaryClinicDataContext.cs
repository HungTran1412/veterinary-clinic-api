using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Data
{
    public partial class VeterinaryClinicDataContext : DbContext
    {
        private readonly IContextAccessor _contextAccessor;

        protected VeterinaryClinicDataContext(Func<IContextAccessor> contextAccessorFactory)
        {
            _contextAccessor = contextAccessorFactory();
        }

        public VeterinaryClinicDataContext(DbContextOptions<VeterinaryClinicDataContext> options) : base(options)
        {
        }

        public DbSet<VcPets> VcPets { get; set; }
        public DbSet<VcUsers> VcUsers { get; set; }
        public DbSet<VcInvoices> VcInvoices { get; set; }
        public DbSet<VcServices> VcServices { get; set; }
        public DbSet<VcEmailLogs> VcEmailLogs { get; set; }
        public DbSet<VcAppointments> VcAppointments { get; set; }
        public DbSet<VcNotifications> VcNotifications { get; set; }
        public DbSet<VcWorkSchedules> VcWorkSchedules { get; set; }
        public DbSet<vcMedicalRecords> VcMedicalRecords { get; set; }
        public DbSet<VcSpecializations> VcSpecializations { get; set; }
        public DbSet<VcDoctorSpecializations> VcDoctorSpecializations { get; set; }
        public DbSet<VcUserVerificationTokens> VcUserVerificationTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
        
        public override int SaveChanges()
        {
            return SaveChangesAsync().Result;
        }
        
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.Entity is TrackedChangeEntity entity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entity.CreatedDate = DateTime.Now;
                            entity.CreatedUserId = _contextAccessor.UserId;
                            entity.CreatedUserName = _contextAccessor.UserName;
                            break;
                        case EntityState.Modified:
                            entity.ModifiedDate = DateTime.Now;
                            entity.ModifiedUserId = _contextAccessor.UserId;
                            entity.ModifiedUserName = _contextAccessor.UserName;
                            break;
                    }
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }   
}