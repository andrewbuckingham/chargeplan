using ChargePlan.Domain.Splines;

public static class Interpolations
{
    public static AlwaysStep Step() => new();
}

public record AlwaysStep : ChargePlan.Domain.Splines.InterpolationFactory
{
    public AlwaysStep() : base(
        InterpolationType.Step,
        InterpolationType.Step,
        InterpolationType.Step,
        InterpolationType.Step,
        InterpolationType.Step,
        InterpolationType.Step
    ) { }
}