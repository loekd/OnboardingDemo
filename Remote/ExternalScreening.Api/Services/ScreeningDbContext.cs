using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ExternalScreening.Api.Services;

public class ScreeningDbContext : DbContext
{
    public DbSet<ScreeningEntity> ScreeningEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //seed some data
        modelBuilder.Entity<ScreeningEntity>().HasData(
            new ScreeningEntity(Guid.Parse("{4275B413-5764-49D1-9707-64858225E5E4}"), "Miles", "Morales", Guid.Parse("{0B8C953D-445B-4D03-8426-2F45D67D83A5}"), null),
            new ScreeningEntity(Guid.Parse("{7AA1D5F0-7B99-4939-806F-49D2597D836A}"), "Chen", "Lu", Guid.Parse("{D00158FC-1D01-4DAD-92DF-69C66AE84111}"), true),
            new ScreeningEntity(Guid.Parse("{4B2BF2E3-94D8-4338-A3DF-F38F28E50FD6}"), "Lei", "Ling", Guid.Parse("{839E4D14-11C6-41CC-ABE6-20FC48D5F822}"), false)
        );
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "ScreeningDb");
    }
}

public class ScreeningEntity
{
    public ScreeningEntity()
    {
        Id = Guid.NewGuid();
    }

    public ScreeningEntity(string firstName, string lastName, Guid onboardingId)
    {
        FirstName = firstName;
        LastName = lastName;
        OnboardingId = onboardingId;
    }

    public ScreeningEntity(Guid id, string firstName, string lastName, Guid onboardingId, bool? isApproved)
        : this(firstName, lastName, onboardingId)
    {
        Id = id;
        IsApproved = isApproved;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid OnboardingId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool? IsApproved { get; set; }
}