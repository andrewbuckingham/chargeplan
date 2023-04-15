public interface IGenerationProfile : ISplineable<GenerationValue>
{
}

public record GenerationValue(DateTime DateTime, float Power);