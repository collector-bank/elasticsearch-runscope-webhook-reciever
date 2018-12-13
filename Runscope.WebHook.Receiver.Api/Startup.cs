using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Collector.Serilog.Enrichers.Assembly;
using Collector.Serilog.Enrichers.Author;
using Collector.Serilog.Sinks.AzureEventHub;
using Destructurama;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Enrichers.AzureWebApps;
using Serilog.Exceptions;

namespace Runscope.WebHook.Receiver.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();

            dynamic settings = LoadAppSettings();

            string eventHubConnectionString = settings.EventHubConnectionString;
            string serilogTeamName = settings?.SerilogTeamName ?? "Unknown team";
            string serilogDepartment = settings?.SerilogDepartment ?? "Unknown department";

            ILogger logger = ConfigureLogging(eventHubConnectionString, serilogTeamName, serilogDepartment);
        }

        static JObject LoadAppSettings()
        {
            string configfile = "appsettings.development.json";
            if (System.IO.File.Exists(configfile))
            {
                return JObject.Parse(System.IO.File.ReadAllText(configfile));
            }

            configfile = "appsettings.json";
            if (System.IO.File.Exists(configfile))
            {
                return JObject.Parse(System.IO.File.ReadAllText(configfile));
            }

            throw new System.IO.FileNotFoundException(configfile);
        }

        ILogger ConfigureLogging(string eventHubConnectionString, string teamName, string department)
        {
            if (!Log.Logger.GetType().Name.Equals("SilentLogger", StringComparison.CurrentCultureIgnoreCase))
            {
                return Log.Logger;
            }

            var config = new LoggerConfiguration()
                .Enrich.With(new AuthorEnricher(
                    teamName: teamName,
                    department: department,
                    repositoryUrl: new Uri("https://github.com/collector-bank/elasticsearch-runscope-webhook-reciever"),
                    serviceGroup: "Logging"))
                .Enrich.With<AzureWebAppsNameEnricher>()
                .Enrich.With<AzureWebJobsNameEnricher>()
                .Enrich.With<SourceSystemEnricher<Program>>()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Destructure.UsingAttributes();

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IsDevelopment")))
            {
                Console.WriteLine("IsDevelopment set: Logging to console");
                config = config
                    .MinimumLevel.Debug()
                    .WriteTo.Console();
            }
            else if (eventHubConnectionString == null)
            {
                Console.WriteLine("EventHubConnectionString missing in appsettings.json: Logging to console");
                config = config
                    .MinimumLevel.Debug()
                    .WriteTo.Console();
            }
            else
            {
                Console.WriteLine($"Using event hub for logging: >>>{eventHubConnectionString}<<<");
                var eventHub = EventHubClient.CreateFromConnectionString(eventHubConnectionString);
                config = config.WriteTo.Sink(new AzureEventHubSink(eventHub));
            }

            ILogger logger = config.CreateLogger();

            string version = GetAppVersion();

            logger.Information("Logger initiliazed: {Version}", version);

            return logger;
        }

        static string GetAppVersion()
        {
            if (typeof(Program).Assembly.GetCustomAttributes(false).SingleOrDefault(o => o.GetType() == typeof(AssemblyFileVersionAttribute)) is AssemblyFileVersionAttribute versionAttribute)
            {
                return versionAttribute.Version;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
