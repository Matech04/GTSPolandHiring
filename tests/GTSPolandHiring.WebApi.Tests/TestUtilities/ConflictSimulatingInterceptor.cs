using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GTSPolandHiring.WebApi.Tests.TestUtilities;

/// <summary>
/// Stands in for a real database's unique-constraint enforcement in tests: the EF Core InMemory
/// provider does not actually reject duplicate values on a unique index, so a genuine race on
/// employee email can't be reproduced by just seeding data. This interceptor injects a "racing"
/// row the first time a save touches the conflicting email, then throws for every save that
/// touches it — exactly like a real duplicate-key error would on each retry.
/// </summary>
public sealed class ConflictSimulatingInterceptor(string dbName, string conflictingEmail) : SaveChangesInterceptor
{
    private bool _injected;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var touchesConflictingEmail = eventData.Context!.ChangeTracker.Entries<Employee>()
            .Any(e => e.State is EntityState.Added or EntityState.Modified &&
                      string.Equals(e.Entity.Email, conflictingEmail, StringComparison.OrdinalIgnoreCase));

        if (!touchesConflictingEmail)
        {
            return base.SavingChangesAsync(eventData, result, ct);
        }

        if (!_injected)
        {
            _injected = true;

            using var racingContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);
            racingContext.Employees.Add(new Employee
            {
                Name = "Racing Employee",
                Email = conflictingEmail,
                HireDate = new DateOnly(2020, 1, 1),
                PhoneNo = "+48000000000",
                Address = "Addr",
                State = "State",
                Country = "Country",
                City = "City",
                Pincode = "00-000"
            });
            racingContext.SaveChanges();
        }

        throw new DbUpdateException("Simulated unique constraint violation for test purposes.");
    }
}
