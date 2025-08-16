using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenCqrs.Bus.ServiceBus.Extensions;
using OpenCqrs.Caching.Memory.Extensions;
using OpenCqrs.Extensions;
using OpenCqrs.Sample.EventSourcing.Domain;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;
using OpenCqrs.Store.Cosmos.Sql.Configuration;
using OpenCqrs.Store.EF.Cosmos.Extensions;
using OpenCqrs.UI.Extensions;
using OpenCqrs.Validation.FluentValidation.Extensions;

namespace OpenCqrs.Sample.EventSourcing
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
            services.AddRazorPages();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<ReportingDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("ReportingDb")));

            services
                .AddOpenCqrs(options => 
                {
                    options.PublishEvents = true;
                    options.SaveCommandData = true;
                }, typeof(Product))
                .AddCosmosStore(options =>
                {
                    options.ConnectionString = Configuration.GetConnectionString("MyDomainStore");
                })
                .AddServiceBus(options => {
                    options.ConnectionString = Configuration.GetConnectionString("MyMessageBus");
                })
                .AddFluentValidation()
                //.AddRedisCache(options => {
                //    options.ConnectionString = Configuration.GetConnectionString("MyRedisCache");
                //})
                .AddMemoryCache()
                .AddUI();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ReportingDbContext dbContext, IOptions<CosmosDbOptions> settings)
        {
            dbContext.Database.EnsureCreated();
            //app.UseOpenCqrs().EnsureCosmosDbSqlDbCreated(settings);
            app.UseOpenCqrs().EnsureCosmosDbCreated();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
