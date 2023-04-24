public record WeatherBuilder(Func<IGenerationProfile> GenerationProfile, double PanelElevation, double PanelAzimuth, double Latitude, double Longitude)
{
    public WeatherBuilder(double PanelElevation, double PanelAzimuth, double Latitude, double Longitude) : this(null, PanelElevation, PanelAzimuth, Latitude, Longitude) { }

    WeatherBuilder WithDniSource(IDirectNormalIrradianceProvider dni)
        => this with
        {
            GenerationProfile = () => new GenerationProfile()
            {
                Values = dni.GetForecast().Select(f =>
                {
                    var sun = Sol.SunPositionRads(f.DateTime, Latitude, Longitude);
                    double irradiatedPower = Sol.DniToIrradiation(f.PowerWatts, PanelAzimuth.ToRads(), PanelElevation.ToRads(), sun.Azimuth.ToRads(), sun.Altitude.ToRads());

                    return new GenerationValue(f.DateTime, (float)irradiatedPower / 1000.0f);
                })
            }
        };

    IGenerationProfile Build() => (GenerationProfile ?? throw new InvalidOperationException())();
}