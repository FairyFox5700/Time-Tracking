using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeTracking.Common.FluentValidator;
using TimeTracking.Common.HttpClientHandler;
using TimeTracking.Common.Jwt;
using TimeTracking.Common.ServiceDiscovery;
using TimeTracking.Common.Services;
using TimeTracking.Common.Swager;
using TimeTracking.ReportGenerator.Bl.Abstract;
using TimeTracking.ReportGenerator.Bl.Impl;
using TimeTracking.ReportGenerator.Bl.Impl.Services;
using TimeTracking.ReportGenerator.Bl.Impl.Validators;

namespace TimeTracking.ReportGenerator.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                })
                .AddFluentValidation(c =>
                {
                    c.RegisterValidatorsFromAssemblyContaining<Startup>();
                    c.RegisterValidatorsFromAssemblyContaining<ReportConfigParameterValidator>();
                });
            services.AddBlLogicServices(Configuration);
            services.AddOptions();
            services.AddJwtAuthServices(Configuration);
            services.AddSingleton(typeof(ILogger), typeof(Logger<Startup>));
            services.AddLogging();
            services.AddSwaggerConfiguration("Time tracking report generator");
            services.AddFluentValidatorServices(Configuration);
            services.RegisterTemplateServices();
            services.AddCors(options =>
            {
                options.AddPolicy(name: "CurrentCorsPolicy",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });
            services.AddHealthChecks();
            services.AddConsulServices(Configuration);
            
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("CurrentCorsPolicy");

            app.UseSwagger();

            if (env.IsDevelopment())
            {
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web App V1");
                });
            }
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }
    }
}
