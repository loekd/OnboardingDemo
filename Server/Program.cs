using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Onboarding.Server;
using Onboarding.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder
    .ConfigureAuth()
    .ConfigureScreeningService();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<OnboardingDbContext>(
        options => options.UseSqlServer("name=ConnectionStrings:DefaultConnection"));
}
else
{
    builder.Services.AddDbContext<OnboardingDbContext>((
        options => options.UseInMemoryDatabase(databaseName: "OnboardingDb"));
}
builder.Services.AddScoped<IOnboardingDataService, OnboardingDataService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
