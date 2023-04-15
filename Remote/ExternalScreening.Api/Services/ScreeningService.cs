
using Microsoft.EntityFrameworkCore;

namespace ExternalScreening.Api.Services;

public interface IScreeningService
{
    Task AddScreening(ScreeningEntity screening);
    ValueTask<ScreeningEntity?> GetScreening(Guid id);
    Task<IEnumerable<ScreeningEntity>> GetScreenings();
    Task UpdateScreeningStatus(ScreeningEntity screening);
}

public class ScreeningService : IScreeningService
{
    private readonly ScreeningDbContext _dbContext;
    private readonly ILogger<ScreeningService> _logger;

    public ScreeningService(ScreeningDbContext dbContext, ILogger<ScreeningService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        dbContext.Database.EnsureCreated();
    }

    public async ValueTask<ScreeningEntity?> GetScreening(Guid id)
    {
        var instance = await _dbContext
            .ScreeningEntities
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == id);

        return instance;
    }

    public Task UpdateScreeningStatus(ScreeningEntity screening)
    {
        _logger.LogTrace("Updating screening {Id}", screening.Id);

        var existing = _dbContext.ScreeningEntities.Single(s => s.Id == screening.Id);

        existing.IsApproved = screening.IsApproved;

        return _dbContext.SaveChangesAsync();
    }

    public Task AddScreening(ScreeningEntity screening)
    {
        _logger.LogTrace("Adding screening {Id}", screening.Id);

        _dbContext.ScreeningEntities.Add(screening);

        return _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ScreeningEntity>> GetScreenings()
    {
        var result = await _dbContext
            .ScreeningEntities
            .AsNoTracking()
            .ToListAsync();
        return result;
    }
}
