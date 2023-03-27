namespace ChargePlan.Service;

public record GenerationProfile
{
    public List<GenerationValue> Values = new();
}

public record GenerationValue(DateTime DateTime, float Power);