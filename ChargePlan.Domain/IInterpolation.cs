namespace ChargePlan.Domain;

public interface IInterpolation
{
    public double Integrate(double a, double b) => 0.0;
    public double Interpolate(double t) => 0.0;
    public double Integrate(DateTimeOffset from, DateTimeOffset to) => this.Integrate(from.AsTotalHours(), to.AsTotalHours());
    public double Interpolate(DateTimeOffset at) => this.Interpolate(at.AsTotalHours());
    public double Average(DateTimeOffset from, DateTimeOffset to, TimeSpan step)
    {
        if (to <= from) throw new ArgumentOutOfRangeException(nameof(to));
        if (step <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(step));

        int count = 0;
        double accumulator = 0.0f;

        DateTimeOffset now = from;
        while (now < to)
        {
            count++;
            accumulator += Interpolate(now);

            now += step;
        }

        double avg = accumulator / count;
        return avg;
    }
}