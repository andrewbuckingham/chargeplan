using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

// var d = new DateTime(2023, 07, 1, 13, 0, 0);

// var solPos = Sol.SunPositionRads(d, 54.528728, 0);

// Console.WriteLine(Sol.DniToIrradiation(1000.0, (45.0).ToRads(), (0.0).ToRads(), solPos.Azimuth, solPos.Altitude));


var serviceProvider = new ServiceCollection()
    .AddLogging()
    .AddHttpClient()
    .BuildServiceProvider();




var datum = new DateTime(2023, 03, 27);

var wfhDemand = new PowerAtAbsoluteTimes(new List<(TimeOnly, float)>()
{
    (TimeOnly.MinValue, 0.3f),
    (new (1,00), 0.3f),
    (new (8,00), 0.5f),
    (new (11,00), 0.5f),
    (new (12,00), 0.5f),
    (new (13,00), 0.5f),
    (new (17,00), 0.5f),
    (new (18,00), 0.6f),
    (new (19,00), 0.7f),
    (new (20,00), 0.7f),
    (new (23,00), 0.3f),
    //(TimeOnly.MaxValue, 0.3f)
});

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

var charge = new PowerAtAbsoluteTimes(new List<(TimeOnly TimeOfDay, float Power)>()
{
    new (TimeOnly.MinValue, 0.0f),
    new (new(00,30), 2.8f),
    new (new(04,30), 0.0f),
    //new (TimeOnly.MaxValue, 0.0f)
});

var pricing = new PriceAtAbsoluteTimes(new List<(TimeOnly TimeOfDay, decimal PricePerUnit)>()
{
    new (TimeOnly.MinValue, 0.3895M),
    new (new(00,30), 0.095M),
    new (new(04,30), 0.3895M),
    //new (TimeOnly.MaxValue, 0.3895M)
});

var export = new PriceAtAbsoluteTimes(new List<(TimeOnly TimeOfDay, decimal PricePerUnit)>()
{
    new (TimeOnly.MinValue, 0.041M)
});

var dishwasherAuto = new PowerAtRelativeTimes(new List<(TimeSpan RelativeTime, float Power)>()
{
    new (TimeSpan.Zero, 1.63f),
    new (TimeSpan.FromHours(0.5), 0.05f),
    new (TimeSpan.FromHours(1.25), 2.2f),
    new (TimeSpan.FromHours(1.5), 0.0f)
}, "Dishwasher Auto");

var dishwasherEco = new PowerAtRelativeTimes(new List<(TimeSpan RelativeTime, float Power)>()
{
    new (TimeSpan.Zero, 1.66f),
    new (TimeSpan.FromHours(0.3), 2.2f),
    new (TimeSpan.FromHours(0.4), 0.05f),
    new (TimeSpan.FromHours(2.25), 2.1f),
    new (TimeSpan.FromHours(2.5), 0.0f)
}, "Dishwasher Eco");

var washingMachine = new PowerAtRelativeTimes(new List<(TimeSpan RelativeTime, float Power)>()
{
    new (TimeSpan.Zero, 1.95f),
    new (TimeSpan.FromHours(0.25), 0.075f),
    new (TimeSpan.FromHours(0.5), 0.25f),
    new (TimeSpan.FromHours(0.6), 0.075f),
    new (TimeSpan.FromHours(0.75), 0.19f),
    new (TimeSpan.FromHours(0.8), 0.0f)
}, "Washing Machine");

var dehumidifiers = new PowerAtRelativeTimes(new List<(TimeSpan RelativeTime, float Power)>()
{
    new (TimeSpan.Zero, 0.5f),
    new (TimeSpan.FromHours(4.0), 0.0f)
}, "Dehumidifiers");

var lunch = new PowerAtRelativeTimes(new List<(TimeSpan RelativeTime, float Power)>()
{
    new (TimeSpan.Zero, 2.0f),
    new (TimeSpan.FromHours(0.75), 0.0f)
}, "Lunch", new(11,30), new(13,30));

var tea = new PowerAtRelativeTimes(new List<(TimeSpan RelativeTime, float Power)>()
{
    new (TimeSpan.Zero, 2.0f),
    new (TimeSpan.FromHours(0.5), 0.0f)
}, "Tea", new(17, 00), new(19, 00));

var generation = await new WeatherBuilder(45.0, 0.0, 54.528728, -1.553050)
    .WithDniSource(new DniProvider(serviceProvider.GetService<IHttpClientFactory>()))
    .WithArrayArea(7 * 1.722f * 1.134f)
    .BuildAsync();

var algorithm = new AlgorithmBuilder(new Hy36(0.8f * 5.2f, 2.8f, 2.8f, 3.6f))
    .WithInitialBatteryEnergy(0.3f)
    .WithGeneration(generation)
//    .WithGeneration(datum, goodSpringDay.Concat(goodSpringDay).Select(f => f / 1000.0f).ToArray())
    .AddShiftableDemandAnyDay(washingMachine, priority: ShiftableDemandPriority.Medium)
    .AddShiftableDemandAnyDay(dishwasherAuto, priority: ShiftableDemandPriority.High)
    .ForEachDay(DateTime.Today, DateTime.Today.AddDays(1))
    .AddDemand(wfhDemand)
    .AddChargeWindow(charge)
    .AddPricing(pricing)
    .AddExportPricing(export)
    .AddShiftableDemand(tea, priority: ShiftableDemandPriority.Essential)
    .AddShiftableDemand(lunch, priority: ShiftableDemandPriority.Essential)
    .Build();

var recommendations = algorithm.DecideStrategy();

Debug.WriteLine(recommendations.Evaluation);
foreach (var shiftableDemand in recommendations.ShiftableDemands)
{
    Debug.WriteLine($"{shiftableDemand.ShiftableDemand.Name}: {shiftableDemand.StartAt} (+£{shiftableDemand.AddedCost})");
}

foreach (var step in recommendations.Evaluation.DebugResults)
{
    Debug.WriteLine($"{step.DateTime}: Demand:{step.DemandEnergy.ToString("F3")} Generation:{step.GenerationEnergy.ToString("F3")} Charge:{step.ChargeEnergy.ToString("F3")} Integral:{step.BatteryEnergy.ToString("F3")} £{step.CumulativeCost.ToString("F2")} Export:{step.ExportEnergy.ToString("F3")}");
}
