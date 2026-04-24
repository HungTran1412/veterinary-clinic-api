using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace VeterinaryClinic.Data
{
    public partial class VeterinaryClinicReadDataContext : DbContext
    {
        // private readonly IActionContextAccessor
        protected VeterinaryClinicReadDataContext()
        {
        }

        public VeterinaryClinicReadDataContext(DbContextOptions<VeterinaryClinicReadDataContext> options) :
            base(options)
        {
            
        }
        
        public virtual DbSet<Permission> Permissions { get; set; }
        public virtual DbSet<VcPets> VcPets { get; set; }
        public virtual DbSet<VcUsers> VcUsers { get; set; }
        public virtual DbSet<VcInvoices> VcInvoices { get; set; }
        public virtual DbSet<VcServices> VcServices { get; set; }
        public virtual DbSet<VcEmailLogs> VcEmailLogs { get; set; }
        public virtual DbSet<VcAppointments> VcAppointments { get; set; }
        public virtual DbSet<VcNotifications> VcNotifications { get; set; }
        public virtual DbSet<VcWorkSchedules> VcWorkSchedules { get; set; }
        public virtual DbSet<VcMedicalRecords> VcMedicalRecords { get; set; }
        public virtual DbSet<VcSpecializations> VcSpecializations { get; set; }
        public virtual DbSet<VcDoctorSpecializations> VcDoctorSpecializations { get; set; }
        public virtual DbSet<VcUserVerificationTokens> VcUserVerificationTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VcDoctorSpecializations>()
                .HasKey(ds => new { ds.DoctorId, ds.SpecializationId });

            OnModelCreatingPartial(modelBuilder);
        }
        
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }   
}