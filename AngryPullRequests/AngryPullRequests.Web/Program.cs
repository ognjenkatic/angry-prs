using AngryPullRequests.Application.AngryPullRequests.Commands;
using AngryPullRequests.Infrastructure;
using AngryPullRequests.Infrastructure.Persistence;
using AngryPullRequests.Web;
using AngryPullRequests.Web.Util.Mappings;
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Reflection;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Services.AddInfrastrucutreServices(builder.Configuration);

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder => containerBuilder.RegisterModule(new WebModule()));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LogoutPath = "/logout";
        opt.LoginPath = "/login";
    });
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateRepositoryCommand).Assembly));
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddNotyf(config =>
{
    config.DurationInSeconds = 5;
    config.IsDismissable = true;
    config.Position = NotyfPosition.TopRight;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseNotyf();

app.MapRazorPages();
app.MapDefaultControllerRoute();

MigrationUtil.MigrateDatabase(app.Services);

await app.RunAsync();