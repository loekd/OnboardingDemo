using Microsoft.EntityFrameworkCore;
using Polly;

namespace Onboarding.Server.Services;

public interface IOnboardingDataService
{
    Task AddOnboarding(OnboardingEntity Onboarding);
    ValueTask<OnboardingEntity?> GetOnboarding(Guid id);
    Task<IEnumerable<OnboardingEntity>> GetOnboardings();
    Task UpdateOnboardingStatus(Guid id, bool isApproved);
}

public class OnboardingDataService : IOnboardingDataService
{
    private readonly OnboardingDbContext _dbContext;
    private readonly ILogger<OnboardingDataService> _logger;

    public OnboardingDataService(OnboardingDbContext dbContext, ILogger<OnboardingDataService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        dbContext.Database.EnsureCreated();
    }

    public ValueTask<OnboardingEntity?> GetOnboarding(Guid id)
    {
        return _dbContext.OnboardingEntities.FindAsync(id);
    }

    public Task UpdateOnboardingStatus(Guid id, bool isApproved)
    {
        _logger.LogTrace("Updating Onboarding {Id} to {Status}", id, isApproved);

        var existing = _dbContext.OnboardingEntities.Single(s => s.Id == id);
        if (isApproved)
        {
            existing.Status = Shared.Status.Passed;
        }
        else
        {
            existing.Status = Shared.Status.NotPassed;
        }

        return _dbContext.SaveChangesAsync();
    }

    public Task AddOnboarding(OnboardingEntity Onboarding)
    {
        _logger.LogTrace("Adding Onboarding {Id}", Onboarding.Id);

        _dbContext.OnboardingEntities.Add(Onboarding);

        return _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<OnboardingEntity>> GetOnboardings()
    {
        var query = await _dbContext.OnboardingEntities.ToListAsync();
        return query;
    }
}
