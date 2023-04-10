using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ExternalScreening.Api.Services;

public class ScreeningDbContext : DbContext
{
    public DbSet<ScreeningEntity> ScreeningEntities { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "AuthorDb");
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

    [Key]
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsApproved { get; set; }
}