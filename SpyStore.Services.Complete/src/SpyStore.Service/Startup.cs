﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpyStore.DAL.Initializers;
using SpyStore.DAL.EF;
using SpyStore.DAL.Repos;
using SpyStore.DAL.Repos.Interfaces;
using SpyStore.Service.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace SpyStore.Service
{
    public class Startup
    {
        private IHostingEnvironment _env;
        public Startup(IHostingEnvironment env)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
            _env = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore(config =>
                config.Filters.Add(new SpyStoreExceptionFilter(_env.IsDevelopment())))
                .AddJsonFormatters(j =>
                {
                    j.ContractResolver = new DefaultContractResolver();
                    j.Formatting = Formatting.Indented;
                });

            // http://docs.asp.net/en/latest/security/cors.html
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials();
                });
            });

            services.AddDbContext<StoreContext>(
                options => options.UseSqlServer(
                    Configuration.GetConnectionString("SpyStore")));

            services.AddScoped<ICategoryRepo, CategoryRepo>();
            services.AddScoped<IProductRepo, ProductRepo>();
            services.AddScoped<ICustomerRepo, CustomerRepo>();
            services.AddScoped<IShoppingCartRepo, ShoppingCartRepo>();
            services.AddScoped<IOrderRepo, OrderRepo>();
            services.AddScoped<IOrderDetailRepo, OrderDetailRepo>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                loggerFactory.AddConsole(Configuration.GetSection("Logging"));
                loggerFactory.AddDebug();
            }

            if (env.IsDevelopment())
            {
                using (var serviceScope = app.ApplicationServices.CreateScope())
                {
                    StoreDataInitializer.InitializeData(serviceScope.ServiceProvider);
                }
            }

            app.UseCors("AllowAll");  // has to go before UseMvc

            app.UseMvc();
        }
    }
}