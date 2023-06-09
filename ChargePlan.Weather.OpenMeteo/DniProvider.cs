using System.Text.Json;

namespace ChargePlan.Weather.OpenMeteo;

public class DniProvider : IDirectNormalIrradianceProvider
{
    private readonly IHttpClientFactory _clientFactory;

    private const string _uri = "https://api.open-meteo.com/v1/forecast?latitude=54.528728&longitude=-1.553050&current_weather=true&hourly=direct_normal_irradiance,diffuse_radiation&forecast_days=3";
    public DniProvider(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<IEnumerable<(DateTime DateTime, float DirectWatts, float? DiffuseWatts)>> GetDniForecastAsync()
    {
        using var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(_uri);

        if (response.IsSuccessStatusCode == false) throw new HttpRequestException($"Request to {_uri} failed with status code {response.StatusCode}");

        var entity = await JsonSerializer.DeserializeAsync<ResponseEntity>(response.Content.ReadAsStream()) ?? throw new InvalidOperationException();

        // Note the OpenMeteo forecast is for the "preceding hour"
        var values = entity.hourly.time
            .Zip(entity.hourly.direct_normal_irradiance, entity.hourly.diffuse_radiation)
            .Select(pair => (DateTime: DateTime.Parse(pair.First + ":00.000Z") - TimeSpan.FromHours(1), DirectWatts: (float)pair.Second, DiffuseWatts: (float?)pair.Third))
            .ToArray();

        return values;
    }
}