using System.Diagnostics;
using ChargePlan.Service;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var datum = new DateTime(2023, 03, 27);

var demand = new DemandValue[]
{
    new (datum, 0.3f),
    new (datum.AddHours(1), 0.3f),
    new (datum.AddHours(8), 0.5f),
    new (datum.AddHours(11), 0.5f),
    new (datum.AddHours(12), 0.8f),
    new (datum.AddHours(13), 0.5f),
    new (datum.AddHours(17), 0.5f),
    new (datum.AddHours(18), 0.5f),
    new (datum.AddHours(18.25), 0.5f),
    new (datum.AddHours(18.5), 2.5f),
    new (datum.AddHours(18.75), 0.5f),
    new (datum.AddHours(19), 0.5f),
    new (datum.AddHours(20), 0.5f),
    new (datum.AddHours(24), 0.3f)
}.ToList();

// var demand = new DemandValue[]
// {
//     new (datum, 0.3f),
//     new (datum.AddHours(1), 0.3f),
//     new (datum.AddHours(8), 2.5f),
//     new (datum.AddHours(11), 2.5f),
//     new (datum.AddHours(12), 1.5f),
//     new (datum.AddHours(13), 1.2f),
//     new (datum.AddHours(17), 1.2f),
//     new (datum.AddHours(18), 0.5f),
//     new (datum.AddHours(18.25), 0.5f),
//     new (datum.AddHours(18.5), 2.5f),
//     new (datum.AddHours(18.75), 0.5f),
//     new (datum.AddHours(19), 0.5f),
//     new (datum.AddHours(20), 0.5f),
//     new (datum.AddHours(24), 0.3f)
// }.ToList();


float[] generationPerHour = new float[]
{
    0,0,0,0,0,0,
    66,984,1507,1960,2213,2248,
    2162,1915,981,472,142,27,
    2,0,0,0,0 };

var charge = new ChargeValue[]
{
    new (datum, 0.0f),
    new (datum.AddHours(1), 3.0f),
    new (datum.AddHours(2), 3.0f),
    new (datum.AddHours(3), 3.0f),
    new (datum.AddHours(4), 0.0f),
    new (datum.AddHours(6), 0.0f),
    new (datum.AddHours(24), 0.0f)
}.ToList();

var pricing = new PricingValue[]
{
    new (datum, 0.40M),
    new (datum.AddHours(1), 0.11M),
    new (datum.AddHours(2), 0.11M),
    new (datum.AddHours(3), 0.11M),
    new (datum.AddHours(4), 0.11M),
    new (datum.AddHours(6), 0.40M),
    new (datum.AddHours(24), 0.40M)
}.ToList();

var algorithm = new AlgorithmBuilder(new StorageProfile(0.8f * 5.2f, 2.8f, 2.8f), new CurrentState(datum, 0.0f))
    .WithDemand(demand)
    .WithCharge(charge)
    .WithPricing(pricing)
    .WithHourlyGeneration(datum, generationPerHour.Select(f => f / 1000.0f).ToArray())
    .Build();

var decision = algorithm.DecideStrategy();

foreach (var step in decision.DebugResults)
{
    Debug.WriteLine($"{step.DateTime.TimeOfDay}: Demand:{step.DemandEnergy.ToString("F3")} Generation:{step.GenerationEnergy.ToString("F3")} Charge:{step.ChargeEnergy.ToString("F3")} Integral:{step.BatteryEnergy.ToString("F3")} £{step.CumulativeCost}");
}

Console.WriteLine($"Recommended charge rate limit: {decision.RecommendedChargeRateLimit?.ToString() ?? "none"}");
