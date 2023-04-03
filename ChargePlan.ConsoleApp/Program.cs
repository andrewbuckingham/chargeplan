using System.Diagnostics;
using ChargePlan.Service;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var datum = new DateTime(2023, 03, 27);

var demand = new DemandValue[]
{
    new (datum, 0.3f),
    new (datum.AddHours(1), 0.3f),
    new (datum.AddHours(8), 0.8f),
    new (datum.AddHours(11), 0.6f),
    new (datum.AddHours(12), 0.7f),
    new (datum.AddHours(13), 0.7f),
    new (datum.AddHours(17), 0.7f),
    new (datum.AddHours(18), 0.6f),
    new (datum.AddHours(19), 0.8f),
    new (datum.AddHours(20), 0.8f),
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


float[] goodSpringDay = new float[]
{
    0,0,0,0,0,0,
    0,350,1000,1900,2300,2500,
    2600,2500,2300,1690,1000,490,
    2,0,0,0,0
};

float[] wintersDay = new float[]
{
    0,0,0,0,0,0,
    0,10,80,100,120,200,
    200,180,120,100,80,10,
    2,0,0,0,0
};

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

var dishwasher = new ShiftableDemandValue[]
{
    new (TimeSpan.Zero, 1.0f),
    new (TimeSpan.FromHours(0.5), 0.2f),
    new (TimeSpan.FromHours(2.0), 1.0f),
    new (TimeSpan.FromHours(2.5), 0.0f)
};

var washingMachine = new ShiftableDemandValue[]
{
    new (TimeSpan.Zero, 0.5f),
    new (TimeSpan.FromHours(2.0), 0.0f)
};

var dehumidifiers = new ShiftableDemandValue[]
{
    new (TimeSpan.Zero, 0.5f),
    new (TimeSpan.FromHours(4.0), 0.0f)
};

var lunch = new ShiftableDemandValue[]
{
    new (TimeSpan.Zero, 2.0f),
    new (TimeSpan.FromHours(0.75), 0.0f)
};

var tea = new ShiftableDemandValue[]
{
    new (TimeSpan.Zero, 2.0f),
    new (TimeSpan.FromHours(0.5), 0.0f)
};

var algorithm = new AlgorithmBuilder(new StorageProfile(0.8f * 5.2f, 2.8f, 2.8f), new CurrentState(datum, 0.0f))
    .WithDemand(demand)
    .WithCharge(charge)
    .WithPricing(pricing)
    .WithHourlyGeneration(datum, wintersDay.Select(f => f / 1000.0f).ToArray())
    .AddShiftableDemand("dishwasher", dishwasher)
    .AddShiftableDemand("washing machine", washingMachine)
    .AddShiftableDemand("washing machine", washingMachine)
    .AddShiftableDemand("washing machine", washingMachine)
    .AddShiftableDemand("dehumidifiers", dehumidifiers)
    .AddShiftableDemand("lunch", lunch, noEarlierThan: new(11, 30), noLaterThan: new(13, 30))
    .AddShiftableDemand("tea", tea, noEarlierThan: new(17, 00), noLaterThan: new(19, 00))
    .Build();

var decision = algorithm.DecideStrategy();

foreach (var step in decision.DebugResults)
{
    Debug.WriteLine($"{step.DateTime.TimeOfDay}: Demand:{step.DemandEnergy.ToString("F3")} Generation:{step.GenerationEnergy.ToString("F3")} Charge:{step.ChargeEnergy.ToString("F3")} Integral:{step.BatteryEnergy.ToString("F3")} £{step.CumulativeCost}");
}

Console.WriteLine($"Recommended charge rate limit: {decision.RecommendedChargeRateLimit?.ToString() ?? "none"}");
