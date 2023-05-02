using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using System.Text.Json;
using Microsoft.Extensions.Options;

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
            .AddSingleton<JsonSerializerOptions>(sp => sp.GetRequiredService<IOptions<JsonSerializerOptions>>().Value)
            .AddAzureClients(configureClients =>
            {
                configureClients.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            });

        services
            .AddScoped<UserRecommendationService>()
            .AddScoped<UserTemplateService>()
            .AddScoped<UserProfileService>()
            .AddScoped<AdhocRecommendationService>()
            .AddSingleton<IDirectNormalIrradianceProvider, DniProvider>()
            .AddSingleton<IPlantFactory, PlantFactory>();

        services
            .AddSingleton<IUserPlantRepository, UserPlantRepository>()
            .AddSingleton<IUserChargeRepository, UserChargeRepository>()
            .AddSingleton<IUserDemandRepository, UserDemandRepository>()
            .AddSingleton<IUserExportRepository, UserExportRepository>()
            .AddSingleton<IUserPricingRepository, UserPricingRepository>()
            .AddSingleton<IUserShiftableDemandRepository, UserShiftableDemandRepository>()
            .AddSingleton<IUserDayTemplatesRepository, UserDayTemplatesRepository>()
            .AddSingleton<IUserDemandCompletedRepository, UserDemandCompletedRepository>();
    })
    .Build();

host.Run();
