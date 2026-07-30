using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder) {
            builder.ToTable("Appointments");

            builder.HasIndex(a => new { a.AvailabilityId }).IsUnique(false);

            builder.Property(a => a.Reason).IsRequired();
            builder.Property(a => a.PatientDni).IsRequired();
            builder.Property(a => a.State).IsRequired();

        }
}
}