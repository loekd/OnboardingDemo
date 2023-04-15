using Duende.IdentityServer;
using Microsoft.AspNetCore.HttpOverrides;

namespace ExternalScreening.Api.IdSrv;

internal static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        //allow X-Forward headers to specify the real host and protocol of Identity Server
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            options.RequireHeaderSymmetry = false;
            options.ForwardLimit = null;
        });

        builder.Services.AddIdentityServer(options =>
            {
                options.AccessTokenJwtType = "JWT";
                if (!builder.Environment.IsDevelopment())
                {
                    //options.IssuerUri = "https://ca-identity-server.politewater-ba7a3a0c.westeurope.azurecontainerapps.io";
                }
            })
            .AddInMemoryApiResources(Config.ApiResources)
            .AddInMemoryApiScopes(Config.ApiScopes)
            .AddInMemoryClients(Config.Clients);

        return builder;
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            //app.UseSwagger();
            //app.UseSwaggerUI();
        }
        
        //app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();

        app.UseIdentityServer();

        app.UseAuthorization();
        app.MapRazorPages().RequireAuthorization();

        return app;
    }
}
