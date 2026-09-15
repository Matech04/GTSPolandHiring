using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTSPolandHiring.WebApi.Features.Employee.Domain;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.Property(e => e.PhoneNo)
            .HasMaxLength(30);

        builder.Property(e => e.ProfilePicture)
            .HasMaxLength(2048); 

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Address)
            .HasMaxLength(255);

        builder.Property(e => e.City)
            .HasMaxLength(100);

        builder.Property(e => e.State)
            .HasMaxLength(100);

        builder.Property(e => e.Country)
            .HasMaxLength(100);

        builder.Property(e => e.Pincode)
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .IsRequired();
    }
}