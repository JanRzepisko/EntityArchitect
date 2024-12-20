using System.Globalization;
using EntityArchitect.Entities;
using EntityArchitect.Entities.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace EntityArchitect.Testing.Fixtures
{
    public class IntegrationTestWebAppFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("db")
            .WithUsername("admin")
            .WithPassword("admin")
            .Build();

        public Task InitializeAsync()
        {
            return _dbContainer.StartAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.AddEnvironmentVariables();
            });

            builder.ConfigureTestServices(services =>
            {
                var connectionString = _dbContainer.GetConnectionString();
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddLogging(builder =>
                {
                    builder.AddFilter("Microsoft.AspNetCore", LogLevel.None); // Wyłączenie EF Core
                });
                
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                    options.UseLoggerFactory(LoggerFactory.Create(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Critical); 
                    }));
                    options.UseSnakeCaseNamingConvention();
                }
                    );

                services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
                
                using (var scope = services.BuildServiceProvider().CreateScope())
                {
                    if (string.IsNullOrEmpty(connectionString)) return;
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    dbContext.Database.EnsureCreated();
                }

            });
        }

        public Task DisposeAsync()
        {
            return _dbContainer.StopAsync();
        }

        public IntegrationTestWebAppFactory()
        {
            
        }
    }
}