namespace ChargePlan.Domain;

public interface IGenerationProfile : ISplineable<GenerationValue>
{
}

public record GenerationValue(DateTimeOffset DateTime, float Power);