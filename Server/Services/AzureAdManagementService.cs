using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Onboarding.Server.Services;

public interface IAzureAdManagementService
{
    Task<AadUser> CreateUser(string firstName, string lastName);
    Task<AadUser?> FindUser(string firstName, string lastName);
}

public class AzureAdManagementService : IAzureAdManagementService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<AzureAdManagementService> _logger;

    public AzureAdManagementService(GraphServiceClient graphServiceClient, ILogger<AzureAdManagementService> logger)
    {
        _graphServiceClient = graphServiceClient ?? throw new ArgumentNullException(nameof(graphServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AadUser?> FindUser(string firstName, string lastName)
    {
        _logger.LogTrace("Searching for user {FirstName}.", firstName);
        try
        {
            var users = await _graphServiceClient
            .Users
            .GetAsync(c =>
            {
                c.QueryParameters.Filter = $"startswith(givenName, '{firstName}') and startswith(surname, '{lastName}')";
                c.QueryParameters.Select = new string[] { "id", "givenName", "surname" };
                c.QueryParameters.Top = 1;
            });

            User? aadUser = users?.Value?.FirstOrDefault();
            return aadUser != null ? new AadUser(aadUser.GivenName!, aadUser.Surname!, aadUser.Id!) : null;
        }
        catch (ODataError ex)
        {
            throw new InvalidOperationException("Failed to communicate to Azure AD", ex);
        }
    }


    public async Task<AadUser> CreateUser(string firstName, string lastName)
    {
        // check for existing user with the same name
        var user = await FindUser(firstName, lastName);

        if (user == null)
        {
            _logger.LogTrace("Creating new user {FirstName}.", firstName);

            var userToCreate = new User
            {
                UserPrincipalName = $"{lastName}.{firstName}@loekd.com".Replace(" ", string.Empty),
                AccountEnabled = true,
                DisplayName = $"{lastName} {firstName}",
                MailNickname = $"{lastName}{firstName}".Replace(" ", string.Empty),
                GivenName = firstName,
                Surname = lastName,
                PasswordProfile = new PasswordProfile
                {
                    Password = Guid.NewGuid().ToString(),
                    ForceChangePasswordNextSignIn = true
                }
            };
            try
            {
                var aadUser = await _graphServiceClient
                                        .Users
                                        .PostAsync(userToCreate);
                if (aadUser == null)
                {
                    throw new InvalidOperationException("Failed to create account in Azure AD");
                }
                user = new AadUser(aadUser.GivenName!, aadUser.Surname!, aadUser.Id!);
            }
            catch (ODataError ex)
            {
                throw new InvalidOperationException("Failed to communicate to Azure AD", ex);
            }
        }

        return user;
    }
}

public record AadUser(string FirstName, string LastName, string Id);
