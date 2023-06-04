namespace ChargePlan.Weather.OpenMeteo;

public class CurrentWeather
{
    public double temperature { get; set; }
    public double windspeed { get; set; }
    public int winddirection { get; set; }
    public int weathercode { get; set; }
    public int is_day { get; set; }
    public string time { get; set; }
}

public class Hourly
{
    public List<string> time { get; set; }
    public List<double> direct_normal_irradiance { get; set; }
    public List<double> diffuse_radiation { get; set; }
}

public class HourlyUnits
{
    public string time { get; set; }
    public string direct_normal_irradiance { get; set; }
}

public class ResponseEntity
{
    public double latitude { get; set; }
    public double longitude { get; set; }
    public double generationtime_ms { get; set; }
    public int utc_offset_seconds { get; set; }
    public string timezone { get; set; }
    public string timezone_abbreviation { get; set; }
    // public int elevation { get; set; }
    // public CurrentWeather current_weather { get; set; }
    // public HourlyUnits hourly_units { get; set; }
    public Hourly hourly { get; set; }
}