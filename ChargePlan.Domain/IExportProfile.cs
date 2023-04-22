public interface IExportProfile : ISplineable<PricingValue>
{
}

public record ExportValue(DateTime DateTime, decimal PricePerUnit);