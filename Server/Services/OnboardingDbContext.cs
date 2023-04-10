using Microsoft.EntityFrameworkCore;
using Onboarding.Shared;
using System.ComponentModel.DataAnnotations;

namespace Onboarding.Server.Services;

public class OnboardingDbContext : DbContext
{
    public DbSet<OnboardingEntity> OnboardingEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //seed some data
        modelBuilder.Entity<OnboardingEntity>().HasData(
            new OnboardingEntity(Guid.NewGuid(), "John", "Doe", Status.Pending),
            new OnboardingEntity(Guid.NewGuid(), "Jane", "Doe", Status.Approved),
            new OnboardingEntity(Guid.NewGuid(), "Jack", "Doe", Status.NotApproved),
            new OnboardingEntity(Guid.NewGuid(), "Joey", "Doe", Status.Skipped)
        );
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "AuthorDb");
    }
}

public class OnboardingEntity
{
    public OnboardingEntity()
    {
    }

    public OnboardingEntity(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public OnboardingEntity(Guid id, string firstName, string lastName, Status status)
        : this(firstName, lastName)
    {
        Id = id;
        Status = status;
    }

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsApproved { get; set; }

    public Status Status { get; set; } = Status.Unknown;
}