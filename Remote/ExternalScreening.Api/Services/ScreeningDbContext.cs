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
            new ScreeningEntity(Guid.Parse("{4275B413-5764-49D1-9707-64858225E5E4}"), "Miles", "Morales", null ),
            new ScreeningEntity(Guid.Parse("{7AA1D5F0-7B99-4939-806F-49D2597D836A}"), "Chen", "Lu", true ),
            new ScreeningEntity(Guid.Parse("{839E4D14-11C6-41CC-ABE6-20FC48D5F822}"), "Lei", "Ling", false)
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

    public ScreeningEntity(string firstName, string lastName)
    { 
        FirstName = firstName;
        LastName = lastName;
    }

    public ScreeningEntity(Guid id, string firstName, string lastName, bool? isApproved)
        : this(firstName, lastName)
    {
        Id = id;
        IsApproved = isApproved;
    }

    [Key]
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool? IsApproved { get; set; }
}