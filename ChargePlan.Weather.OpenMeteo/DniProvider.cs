using System.Text.Json;

namespace ChargePlan.Weather.OpenMeteo;

public class DniProvider : IDirectNormalIrradianceProvider
{
    private readonly IHttpClientFactory _clientFactory;

    private const string _uri = "https://api.open-meteo.com/v1/forecast?latitude=54.528728&longitude=-1.553050&current_weather=true&hourly=direct_normal_irradiance,diffuse_radiation,cloudcover&forecast_days=3";
    public DniProvider(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<IEnumerable<DniValue>> GetDniForecastAsync()
    {
        using var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(_uri);

        if (response.IsSuccessStatusCode == false) throw new HttpRequestException($"Request to {_uri} failed with status code {response.StatusCode}");

        var entity = await JsonSerializer.DeserializeAsync<ResponseEntity>(response.Content.ReadAsStream()) ?? throw new InvalidOperationException();

        // Note the OpenMeteo forecast is for the "preceding hour"
        var values = entity.hourly.time
            .Zip(entity.hourly.direct_normal_irradiance, entity.hourly.diffuse_radiation, entity.hourly.cloudcover)
            .Select(items => new DniValue(
                DateTime: DateTime.Parse(items.First + ":00.000Z"),
                DirectWatts: (float)items.Second,
                DiffuseWatts: (float?)items.Third,
                CloudCoverPercent: items.Fourth)
            )
            .ToArray();

        return values;
    }
}

public static class LinqExtensions
{
    public static IEnumerable<(TFirst First, TSecond Second, TThird Third, TFourth Fourth)> Zip<TFirst, TSecond, TThird, TFourth>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third, IEnumerable<TFourth> fourth)
        => first.Zip(second,third).Zip(fourth).Select(f=>(f.First.First, f.First.Second, f.First.Third, f.Second));
}