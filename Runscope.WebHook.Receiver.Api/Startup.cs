using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using System;

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
            services.AddControllers();

            services.Configure<ElasticSettings>(Configuration.GetSection("Elasticsearch"));
            services.AddTransient(a => new ApiKey() { Key = Configuration["ApiKey"] });
            services.AddTransient(a => new AgentRegionName() { RegionName = Configuration["AgentRegionName"] });
            services.AddHttpClient<ElasticConnector>()
                    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(600)));

            var appInsightsInstrumentationKey = Configuration["Logging:ApplicationInsights:InstrumentationKey"];
            services.AddApplicationInsightsTelemetry(appInsightsInstrumentationKey);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
