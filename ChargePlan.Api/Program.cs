using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => services
        .AddHttpClient()
        .Configure<JsonSerializerOptions>(options =>
        {
            options.AllowTrailingCommas = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
            options.IncludeFields = true;
            // options.Converters.Add(new DateOnlyConverter());
            // options.Converters.Add(new TimeOnlyConverter());        
        })
        .AddScoped<RecommendationService>()
        .AddSingleton<IDirectNormalIrradianceProvider, DniProvider>()
        .AddSingleton<IPlant, Hy36>(_ => new Hy36(0.8f * 5.2f, 2.8f, 2.8f, 3.6f))
    )
    .Build();

host.Run();
