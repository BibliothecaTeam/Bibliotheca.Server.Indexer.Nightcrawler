using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Bibliotheca.Server.ServiceDiscovery.ServiceClient.Extensions;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Parameters;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Services;
using Microsoft.AspNetCore.Http;
using Hangfire;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Jobs;
using Bibliotheca.Server.Mvc.Middleware.Authorization.UserTokenAuthentication;
using Bibliotheca.Server.Indexer.Nightcrawler.Api.UserTokenAuthorization;
using Bibliotheca.Server.Mvc.Middleware.Authorization.SecureTokenAuthentication;
using Bibliotheca.Server.Mvc.Middleware.Authorization.BearerAuthentication;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;
using Swashbuckle.AspNetCore.Swagger;
using System.Net.Http;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Api
{
    /// <summary>
    /// Startup class.
    /// </summary>
    public class Startup
    {
        private IConfigurationRoot Configuration { get; }

        private bool UseServiceDiscovery { get; set; } = true;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="env">Environment parameters.</param>
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        /// <summary>
        /// Service configuration.
        /// </summary>
        /// <param name="services">List of services.</param>
        /// <returns>Service provider.</returns>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ApplicationParameters>(Configuration);

            if (UseServiceDiscovery)
            {
                services.AddHangfire(x => x.UseStorage(new Hangfire.MemoryStorage.MemoryStorage()));
            }

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            services.AddMvc(config =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(SecureTokenDefaults.AuthenticationScheme)
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            }).AddJsonOptions(options =>
            {
                options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            });

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new QueryStringOrHeaderApiVersionReader("api-version");
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "Indexer AzureSearch API",
                    Description = "Microservice for Azure search feature for Bibliotheca.",
                    TermsOfService = "None"
                });

                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "Bibliotheca.Server.Indexer.Nightcrawler.Api.xml"); 
                options.IncludeXmlComments(xmlPath);
            });

            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = Configuration["CacheConfiguration"];
                options.InstanceName = Configuration["CacheInstanceName"];
                options.ResolveDns();
            });

            services.AddServiceDiscovery();
            services.AddSingleton<HttpClient, HttpClient>();

            services.AddScoped<IServiceDiscoveryRegistrationJob, ServiceDiscoveryRegistrationJob>();
            services.AddScoped<IUserTokenConfiguration, UserTokenConfiguration>();

            services.AddScoped<IIndexRefreshJob, IndexRefreshJob>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            
            services.AddScoped<IDiscoveryService, DiscoveryService>();
            services.AddScoped<IGatewayService, GatewayService>();
            services.AddScoped<IQueuesService, QueuesService>();
        }

        /// <summary>
        /// Configure web application.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="env">Environment parameters.</param>
        /// <param name="loggerFactory">Logger.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if(env.IsDevelopment())
            {
                loggerFactory.AddConsole(Configuration.GetSection("Logging"));
                loggerFactory.AddDebug();
            }
            else
            {
                loggerFactory.AddAzureWebAppDiagnostics();
            }

            if (UseServiceDiscovery)
            {
                app.UseHangfireServer();
                RecurringJob.AddOrUpdate<IServiceDiscoveryRegistrationJob>("register-service", x => x.RegisterServiceAsync(null), Cron.Minutely);
            }

            app.UseExceptionHandler();

            app.UseCors("AllowAllOrigins");

            var secureTokenOptions = new SecureTokenOptions
            {
                SecureToken = Configuration["SecureToken"],
                AuthenticationScheme = SecureTokenDefaults.AuthenticationScheme,
                Realm = SecureTokenDefaults.Realm
            };
            app.UseSecureTokenAuthentication(secureTokenOptions);

            var userTokenOptions = new UserTokenOptions
            {
                AuthenticationScheme = UserTokenDefaults.AuthenticationScheme,
                Realm = UserTokenDefaults.Realm
            };
            app.UseUserTokenAuthentication(userTokenOptions);

            var jwtBearerOptions = new JwtBearerOptions
            {
                Authority = Configuration["OAuthAuthority"],
                Audience = Configuration["OAuthAudience"],
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
            };
            app.UseBearerAuthentication(jwtBearerOptions);

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            });
        }
    }
}
