
using Microsoft.EntityFrameworkCore;

namespace ExternalScreening.Api.Services;

public interface IScreeningService
{
    Task AddScreening(ScreeningEntity screening);
    ValueTask<ScreeningEntity?> GetScreening(Guid id);
    Task<IEnumerable<ScreeningEntity>> GetScreenings();
    Task UpdateScreeningStatus(Guid id, bool isApproved);
}

public class ScreeningService : IScreeningService
{
    private readonly ScreeningDbContext _dbContext;
    private readonly ILogger<ScreeningService> _logger;

    public ScreeningService(ScreeningDbContext dbContext, ILogger<ScreeningService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask<ScreeningEntity?> GetScreening(Guid id)
    {
        return _dbContext.ScreeningEntities.FindAsync(id);
    }

    public Task UpdateScreeningStatus(Guid id, bool isApproved)
    {
        _logger.LogTrace("Updating screening {Id} to {Status}", id, isApproved);

        var existing = _dbContext.ScreeningEntities.Single(s => s.Id == id);
        existing.IsApproved = isApproved;

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
        var result = await _dbContext.ScreeningEntities.ToListAsync();
        return result;
    }
}
