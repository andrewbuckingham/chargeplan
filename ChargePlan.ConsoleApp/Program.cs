using System.Diagnostics;

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
    new (datum.AddHours(0.5), 3.0f),
    new (datum.AddHours(4.5), 0.0f),
    new (datum.AddHours(24), 0.0f)
};

var pricing = new PricingValue[]
{
    new (datum, 0.3895M),
    new (datum.AddHours(0.5), 0.095M),
    new (datum.AddHours(4.5), 0.3895M),
    new (datum.AddHours(24), 0.3895M)
};

var export = new ExportValue[]
{
    new (datum, 0.041M)
};

var dishwasherAuto = new ShiftableDemandValue[]
{
    new (TimeSpan.Zero, 1.63f),
    new (TimeSpan.FromHours(0.5), 0.05f),
    new (TimeSpan.FromHours(1.25), 2.2f),
    new (TimeSpan.FromHours(1.5), 0.0f)
};

var dishwasherEco = new ShiftableDemandValue[]
{
    new (TimeSpan.Zero, 1.66f),
    new (TimeSpan.FromHours(0.3), 2.2f),
    new (TimeSpan.FromHours(0.4), 0.05f),
    new (TimeSpan.FromHours(2.25), 2.1f),
    new (TimeSpan.FromHours(2.5), 0.0f)
};

var washingMachine = new ShiftableDemandValue[]
{
    new (TimeSpan.Zero, 1.95f),
    new (TimeSpan.FromHours(0.25), 0.075f),
    new (TimeSpan.FromHours(0.5), 0.25f),
    new (TimeSpan.FromHours(0.6), 0.075f),
    new (TimeSpan.FromHours(0.75), 0.19f),
    new (TimeSpan.FromHours(0.8), 0.0f)
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

var algorithm = new AlgorithmBuilder(new Hy36(0.8f * 5.2f, 2.8f, 2.8f, 3.6f))
    .WithInitialBatteryEnergy(0.3f)
    .WithDemand(demand)
    .WithCharge(charge)
    .WithPricing(pricing)
    .WithExport(export)
    .WithHourlyGeneration(datum, goodSpringDay.Select(f => f / 1000.0f).ToArray())
    .AddShiftableDemand("dishwasher", dishwasherAuto, priority: ShiftableDemandPriority.High)
    .AddShiftableDemand("washing machine", washingMachine, priority: ShiftableDemandPriority.Medium)
    .AddShiftableDemand("dehumidifiers", dehumidifiers, priority: ShiftableDemandPriority.Low)
    .AddShiftableDemand("lunch", lunch, noEarlierThan: new(11, 30), noLaterThan: new(13, 30), priority: ShiftableDemandPriority.Essential)
    .AddShiftableDemand("tea", tea, noEarlierThan: new(17, 00), noLaterThan: new(19, 00), priority: ShiftableDemandPriority.Essential)
    .Build();

var recommendations = algorithm.DecideStrategy();
Debug.WriteLine(recommendations.Evaluation);
foreach (var shiftableDemand in recommendations.ShiftableDemands)
{
    Debug.WriteLine($"{shiftableDemand.ShiftableDemand.Name}: {shiftableDemand.StartAt.TimeOfDay} (+£{shiftableDemand.AddedCost})");
}

foreach (var step in recommendations.Evaluation.DebugResults)
{
    Debug.WriteLine($"{step.DateTime.TimeOfDay}: Demand:{step.DemandEnergy.ToString("F3")} Generation:{step.GenerationEnergy.ToString("F3")} Charge:{step.ChargeEnergy.ToString("F3")} Integral:{step.BatteryEnergy.ToString("F3")} £{step.CumulativeCost.ToString("F2")} Export:{step.ExportEnergy.ToString("F3")}");
}
