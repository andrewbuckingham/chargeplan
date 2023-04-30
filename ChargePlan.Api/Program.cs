using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using System.Text.Json;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services
            .AddHttpClient()
            .Configure<JsonSerializerOptions>(options =>
            {
                options.AllowTrailingCommas = true;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNameCaseInsensitive = true;
                options.IncludeFields = true;
            })
            .AddAzureClients(configureClients =>
            {
                configureClients.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            });

        services
            .AddScoped<RecommendationService>()
            .AddScoped<UserTemplateService>()
            .AddSingleton<IDirectNormalIrradianceProvider, DniProvider>()
            .AddSingleton<IPlant, Hy36>(_ => new Hy36(0.8f * 5.2f, 2.8f, 2.8f, 3.6f))
            .AddSingleton<IUserChargeRepository, UserChargeRepository>()
            .AddSingleton<IUserDemandRepository, UserDemandRepository>()
            .AddSingleton<IUserExportRepository, UserExportRepository>()
            .AddSingleton<IUserPricingRepository, UserPricingRepository>()
            .AddSingleton<IUserShiftableDemandRepository, UserShiftableDemandRepository>();
    })
    .Build();

host.Run();
