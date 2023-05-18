using Microsoft.EntityFrameworkCore;
using Polly;

namespace Onboarding.Server.Services;

public interface IOnboardingDataService
{
    Task AddOnboarding(OnboardingEntity Onboarding);
    Task DeleteOnboarding(OnboardingEntity onboarding);
    Task<OnboardingEntity?> GetOnboarding(Guid id);
    Task<IEnumerable<OnboardingEntity>> GetOnboardings();
    Task UpdateOnboarding(OnboardingEntity onboarding);
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

    public Task<OnboardingEntity?> GetOnboarding(Guid id)
    {
        return _dbContext
            .OnboardingEntities
            .AsNoTracking()
            .Where(o => o.Id == id)
            .SingleOrDefaultAsync();
    }

    public async Task UpdateOnboarding(OnboardingEntity onboarding)
    {
        _logger.LogTrace("Updating Onboarding {Id} to {Status}", onboarding.Id, onboarding.Status);

        var existing = (await _dbContext.OnboardingEntities.FindAsync(onboarding.Id)) ?? throw new InvalidOperationException($"Failed to find Onboarding with Id {onboarding.Id}.");
        existing.Status = onboarding.Status;
        existing.ExternalScreeningId = onboarding.ExternalScreeningId;
        existing.AzureAdAccountId = onboarding.AzureAdAccountId;

        await _dbContext.SaveChangesAsync();
    }

    public Task AddOnboarding(OnboardingEntity Onboarding)
    {
        _logger.LogTrace("Adding Onboarding {Id}", Onboarding.Id);

        _dbContext.OnboardingEntities.Add(Onboarding);

        return _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<OnboardingEntity>> GetOnboardings()
    {
        _logger.LogTrace("Fetching Onboardings");

        var query = await _dbContext
            .OnboardingEntities
            .AsNoTracking()
            .ToListAsync();
        return query;
    }

    public Task DeleteOnboarding(OnboardingEntity onboarding)
    {
        _logger.LogTrace("Removing Onboarding {Id}", onboarding.Id);

        _dbContext.OnboardingEntities.Remove(onboarding);
        return _dbContext.SaveChangesAsync();
    }
}
