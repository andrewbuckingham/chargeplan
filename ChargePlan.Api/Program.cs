using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ChargePlan.Api.Middleware;
using ChargePlan.Api.Auth;
using ChargePlan.Weather;
using ChargePlan.Service.Facades;
using ChargePlan.Service.Infrastructure;
using ChargePlan.Service;
using ChargePlan.Domain;
using ChargePlan.Weather.OpenMeteo;
using ChargePlan.Domain.Plant;
using ChargePlan.Infrastructure.AzureBlob.User;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(worker =>
    {
        worker.UseMiddleware<ExceptionMiddleware>();
        worker.UseMiddleware<AuthMiddleware>();
    })
    .ConfigureServices(services =>
    {
        services
            .AddHttpClient()
            .Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.AllowTrailingCommas = true;
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
                options.SerializerOptions.IncludeFields = true;
            })
            .AddSingleton<JsonSerializerOptions>(sp => sp.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions)
            .AddAzureClients(configureClients =>
            {
                configureClients.AddBlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

                configureClients
                    .AddQueueServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"))
                    .ConfigureOptions(c => c.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64);
            });

        // Auth
        services
            .AddScoped<UserIdAccessor>()
            .AddScoped<IUserIdAccessor>(sp => sp.GetRequiredService<UserIdAccessor>())
            .AddScoped<UserPermissionsFacade>();

        // Service layer
        services
            .AddScoped<UserRecommendationService>()
            .AddScoped<UserTemplateService>()
            .AddScoped<UserProfileService>()
            .AddScoped<AdhocRecommendationService>()
            .AddScoped<ForecastTuningService>()
            .AddSingleton<IDirectNormalIrradianceProvider, DniProvider>()
            .AddSingleton<IPlantFactory, PlantFactory>()
            .AddSingleton<IInterpolationFactory, ChargePlan.Domain.Splines.InterpolationFactory>();

        // Repos
        services
            .AddSingleton<IUserRepositories, UserRepositories>()
            .AddSingleton<IUserAuthorisationRepository, UserAuthorisationRepository>()
            .AddSingleton<IUserPlantRepository, UserPlantRepository>()
            .AddSingleton<IUserChargeRepository, UserChargeRepository>()
            .AddSingleton<IUserDemandRepository, UserDemandRepository>()
            .AddSingleton<IUserExportRepository, UserExportRepository>()
            .AddSingleton<IUserPricingRepository, UserPricingRepository>()
            .AddSingleton<IUserShiftableDemandRepository, UserShiftableDemandRepository>()
            .AddSingleton<IUserDayTemplatesRepository, UserDayTemplatesRepository>()
            .AddSingleton<IUserDemandCompletedRepository, UserDemandCompletedRepository>()
            .AddSingleton<IUserRecommendationsRepository, UserRecommendationsRepository>()
            .AddSingleton<IForecastHistoryRepository, ForecastHistoryRepository>()
            .AddSingleton<IEnergyHistoryRepository, EnergyHistoryRepository>();
    })
    .Build();

host.Run();
