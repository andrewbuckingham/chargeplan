public record WeatherBuilder(Func<float, Task<IGenerationProfile>> FetchGenerationProfile, double PanelElevation, double PanelAzimuth, double Latitude, double Longitude, float IrradianceToPowerScalar)
{
    public WeatherBuilder(double PanelElevation, double PanelAzimuth, double Latitude, double Longitude) : this(Unset, PanelElevation, PanelAzimuth, Latitude, Longitude, 1.0f) { }

    private static Task<IGenerationProfile> Unset(float _) => throw new InvalidStateException("Please add a weather source");

    public WeatherBuilder WithDniSource(IDirectNormalIrradianceProvider dni)
        => this with
        {
            FetchGenerationProfile = async (float irradianceScalar) => new GenerationProfile()
            {
                Values = (await dni.GetForecastAsync()).Select(f =>
                {
                    var sun = Sol.SunPositionRads(f.DateTime, Latitude, Longitude);
                    double irradiatedPower = Sol.DniToIrradiation(f.PowerWatts, PanelAzimuth.ToRads(), PanelElevation.ToRads(), sun.Azimuth.ToRads(), sun.Altitude.ToRads());

                    return new GenerationValue(f.DateTime, irradianceScalar * (float)irradiatedPower / 1000.0f);
                })
            }
        };

    public WeatherBuilder WithArrayArea(float areaSqm, float efficiencyPercent = 20.5f)
        => this with { IrradianceToPowerScalar = areaSqm * efficiencyPercent / 100.0f };

    public Task<IGenerationProfile> BuildAsync() => FetchGenerationProfile(IrradianceToPowerScalar);
}