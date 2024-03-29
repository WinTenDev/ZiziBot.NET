using System;
using LiteDB.Async;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Framework.Extensions;
using WinTenDev.Zizi.Hangfire;
using WinTenDev.Zizi.Utils.Extensions;
using WinTenDev.ZiziMirror.AppHost.Extensions;

namespace WinTenDev.ZiziMirror.AppHost
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            //ServiceAccountService.GenerateServiceAccount(2);
            //ServiceAccountService.ListServiceAccounts("zizibot-295007");

            Configuration = configuration;
            _environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMapConfiguration(Configuration);

            // services
            //     .AddTransient<ZiziMirror>()
            //     .Configure<BotOptions<ZiziMirror>>(Configuration.GetSection(nameof(ZiziMirror)));
            services.AddZiziBot();

            services.AddScoped(_ => new LiteDatabaseAsync("Filename=Storage/Data/Local_LiteDB.db;Connection=shared;"));
            // services.AddDbContext<AuthorizationContext>(builder => builder.UseSqlite(Configuration.GetConnectionString("LocalContext")));
            // services.AddDbContext<AuthorizationContext>(builder => {
            // var connStr = Configuration.GetConnectionString("MysqlLocal");
            // builder.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
            // });
            services.AddDatabaseDeveloperPageExceptionFilter();

            // services.RegisterAssemblyPublicNonGenericClasses()
            //     .Where(type => type.FullName.Contains("Services"))
            //     .AsPublicImplementedInterfaces(ServiceLifetime.Scoped);
            //
            // services.RegisterAssemblyPublicNonGenericClasses()
            //     .Where(type => type.FullName.Contains("Handlers"))
            //     .AsPublicImplementedInterfaces(ServiceLifetime.Scoped);

            services.AddEntityFrameworkMigrations();
            services.AddSqlKataMysql();

            services.Scan(selector => {
                selector.FromCallingAssembly()
                    .FromApplicationDependencies(assembly =>
                        assembly.FullName.StartsWith("WinTenDev"))
                    .AddClasses(filter => filter.Where(type =>
                            type.FullName.Contains("Handlers")
                            || type.FullName.Contains("Services")
                            || !type.FullName.Contains("TelegramService")
                            || !type.FullName.Contains("BotService")
                        )
                    )
                    .AsSelf()
                    .WithScopedLifetime();
            });

            // services.AddScoped<AuthService>();
            // services.AddScoped<HeirroService>();

            // services.AddScoped<AuthorizeCommand>();
            // services.AddScoped<UnAuthorizeCommand>();
            // services.AddScoped<UnAuthorizeAllCommand>();
            //
            // services.AddScoped<ExceptionHandler>();
            // services.AddScoped<NewUpdateHandler>();
            //
            // services.AddScoped<AllDebridCommand>();
            // services.AddScoped<FastDebridCommand>();
            // services.AddScoped<RDebridCommand>();
            //
            // services.AddScoped<CloneCommand>();
            // services.AddScoped<PingCommand>();

            services.AddGoogleDrive();

            services.AddHangfireServerAndConfig();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            var botBuilder = CommandBuilderExtension.ConfigureBot();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            // else
            // {
            // app.UseTelegramBotWebhook<ZiziMirror>(botBuilder);
            // }

            app.UseTelegramBotLongPolling<ZiziMirror>(botBuilder, TimeSpan.FromSeconds(2));

            // app.UseHangfireDashboardAndServer();

            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); }); });
        }
    }
}