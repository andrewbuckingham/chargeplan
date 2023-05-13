using System.Text.Json;

namespace ChargePlan.Weather.OpenMeteo;

public class DniProvider : IDirectNormalIrradianceProvider
{
    private IHttpClientFactory _clientFactory;

    private const string _uri = "https://api.open-meteo.com/v1/forecast?latitude=54.528728&longitude=-1.553050&current_weather=true&hourly=direct_normal_irradiance&forecast_days=3";
    private const float _fudgeFactor = 1.2f;

    public DniProvider(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<IEnumerable<(DateTime DateTime, float PowerWatts)>> GetForecastAsync()
    {
        using var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(_uri);

        if (response.IsSuccessStatusCode == false) throw new HttpRequestException($"Request to {_uri} failed with status code {response.StatusCode}");

        var entity = await JsonSerializer.DeserializeAsync<ResponseEntity>(response.Content.ReadAsStream()) ?? throw new InvalidOperationException();

        var values = entity.hourly.time
            .Zip(entity.hourly.direct_normal_irradiance)
            .Select(pair => (DateTime: DateTime.Parse(pair.First), PowerWatts: (float)pair.Second * _fudgeFactor))
            .ToArray();

        return values;
    }
}