using AutoWrapper;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseSerilog(
        (
            context,
            provider,
            logger
        ) => logger.AddSerilogBootstrapper(provider)
    );

builder.Configuration.AddAppSettingsJson();

// Add services to the container.
builder.Services.MappingAppSettings();
builder.Services.AddAutoMapper(
    expression => {
        expression.AddMaps("WinTenDev.Zizi.Models");
    }
);

builder.Services.AddCacheTower();

builder.Services.AddLiteDb();

builder.Services.AddTelegramBotClient();
builder.Services.AddWtTelegramApi();
builder.Services.AddHostedServices();
builder.Services.AddCommonService();
// builder.Services.AddInjectables();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHangfireServerAndConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(
        options => {
            options.DocumentTitle = "WinTenDev Zizi API";
        }
    );
}

// app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

app.UseRequestTimestamp();

app.UseAuthorization();
app.RunMongoDbPreparation();

app.UseHangfireDashboardAndServer();

app.UseApiResponseAndExceptionWrapper(
    new AutoWrapperOptions()
    {
        IsDebug = true,
        ShowStatusCode = true,
        BypassHTMLValidation = true
    }
);
app.MapControllers();

await app.RunAsync();