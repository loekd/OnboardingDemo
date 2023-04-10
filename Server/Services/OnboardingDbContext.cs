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
            new OnboardingEntity(Guid.Parse("{0B8C953D-445B-4D03-8426-2F45D67D83A5}"), "Miles", "Morales", Status.Pending, "/media/man01.png", Guid.Parse("{4275B413-5764-49D1-9707-64858225E5E4}")),
            new OnboardingEntity(Guid.Parse("{D00158FC-1D01-4DAD-92DF-69C66AE84111}"), "Chan", "Lu", Status.Passed, "/media/man02.png", Guid.Parse("{7AA1D5F0-7B99-4939-806F-49D2597D836A}")),
            new OnboardingEntity(Guid.Parse("{839E4D14-11C6-41CC-ABE6-20FC48D5F822}"), "Lei", "Ling", Status.NotPassed, "/media/woman01.png", Guid.Parse("{4B2BF2E3-94D8-4338-A3DF-F38F28E50FD6}")),
            new OnboardingEntity(Guid.Parse("{9DE040AF-3CB2-48BD-A5CA-D154BE484C5A}"), "Anissa", "Pierce", Status.Skipped, "/media/woman02.png")
        );
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "OnboardingDb");
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

    public OnboardingEntity(Guid id, string firstName, string lastName, Status status, string? imageUrl = null, Guid? externalScreeningId = null)
        : this(firstName, lastName)
    {
        Id = id;
        Status = status;
        ImageUrl = imageUrl;
        ExternalScreeningId = externalScreeningId;
    }

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public Status Status { get; set; } = Status.Unknown;

    public string? ImageUrl { get; set; }

    public Guid? ExternalScreeningId { get; set; }
}