namespace ChargePlan.Domain;

public interface IInterpolation
{
    public double Integrate(double a, double b) => 0.0;
    public double Interpolate(double t) => 0.0;
}