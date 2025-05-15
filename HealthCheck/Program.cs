using HealthCheck.Domain.Models;
using HealthCheck.Domain.Services;
using HealthCheck.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddCors(options =>
{
    options.AddPolicy("SwitchPolicy",
                    policy =>
                    {
                        policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IConfigurationManager, ConfigurationManager>();
builder.Services.AddScoped<LocalService>();
builder.Services.AddScoped<HealthService>();
builder.Services.AddScoped<HealthSettings>(sp =>
{
    var localService = sp.GetRequiredService<LocalService>();
    var settings = localService.MapSettings(configuration);
    return settings;
});

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseCors("SwitchPolicy");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
