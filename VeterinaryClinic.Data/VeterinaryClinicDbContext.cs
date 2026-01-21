using Microsoft.EntityFrameworkCore;

namespace VeterinaryClinic.Data;

public class VeterinaryClinicDbContext : DbContext
{
    public VeterinaryClinicDbContext(DbContextOptions<VeterinaryClinicDbContext> options) : base(options)
    {
    }

    public DbSet<Pet> Pets { get; set; }
}