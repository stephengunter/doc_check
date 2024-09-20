using Serilog;
using Serilog.Events;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ApplicationCore.DataAccess;
using Microsoft.AspNetCore.Identity;
using ApplicationCore.Models;
using ApplicationCore.Settings;
using ApplicationCore.Consts;
using ApplicationCore.DI;
using Web.Filters;
using System.Text.Json.Serialization;
using Infrastructure.Helpers;
using QuestPDF.Infrastructure;
using Infrastructure.Services;

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
	.Enrich.FromLogContext()
	.WriteTo.Console()
	.CreateBootstrapLogger();

try
{
	Log.Information("Starting web application");
	var builder = WebApplication.CreateBuilder(args);
	var Configuration = builder.Configuration;
	builder.Host.UseSerilog((context, services, configuration) => configuration
			.ReadFrom.Configuration(context.Configuration)
			.ReadFrom.Services(services)
			.Enrich.FromLogContext());

	#region Autofac
	builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
	builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
	{
		builder.RegisterModule<ApplicationCoreModule>();
	});
   #endregion

   #region Add Configurations
   builder.Services.Configure<AppSettings>(Configuration.GetSection(SettingsKeys.App));
   builder.Services.Configure<DbSettings>(Configuration.GetSection(SettingsKeys.Db));
   
   #endregion

   string connectionString = Configuration.GetConnectionString("Default")!;
	bool usePostgreSql = Configuration[$"{SettingsKeys.Db}:Provider"].EqualTo(DbProvider.PostgreSql);
   if (usePostgreSql)
	{
      builder.Services.AddDbContext<DefaultContext>(options =>
                  options.UseNpgsql(connectionString));
   }
	else
	{
      builder.Services.AddDbContext<DefaultContext>(options =>
                  options.UseSqlServer(connectionString));
   }
	

   #region AddFilters
   builder.Services.AddScoped<DevelopmentOnlyFilter>();
	#endregion

   

   builder.Services.AddCorsPolicy(Configuration);
   builder.Services.AddControllers()
	.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });


	builder.Services.AddSwagger(Configuration);

   QuestPDF.Settings.License = LicenseType.Community;

   var app = builder.Build();
	if (usePostgreSql) 
	{
      AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
   }
   app.UseDefaultFiles();
   app.UseStaticFiles();

   app.UseSerilogRequestLogging(); 

	if (app.Environment.IsDevelopment())
	{
		app.UseSwagger();
		app.UseSwaggerUI();
	}
	else
	{
		app.UseHttpsRedirection();
	}
   
   

   app.UseCors();
   app.UseAuthentication();
   app.UseAuthorization();

	app.MapControllers();
   app.MapFallbackToFile("/index.html");
   app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
	Log.Information("finally");
	Log.CloseAndFlush();
}






