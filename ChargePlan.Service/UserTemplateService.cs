using ChargePlan.Builder.Templates;
using ChargePlan.Domain.Exceptions;
using ChargePlan.Service.Entities;
using ChargePlan.Service.Facades;
using ChargePlan.Service.Infrastructure;

namespace ChargePlan.Service;

public class UserTemplateService
{
    private readonly UserPermissionsFacade _user;

    private readonly IUserDemandRepository _demand;
    private readonly IUserShiftableDemandRepository _shiftable;
    private readonly IUserChargeRepository _charge;
    private readonly IUserPricingRepository _pricing;
    private readonly IUserExportRepository _export;
    private readonly IUserDayTemplatesRepository _days;

    public UserTemplateService(
        UserPermissionsFacade user,
        IUserDemandRepository demand,
        IUserShiftableDemandRepository shiftable,
        IUserChargeRepository charge,
        IUserPricingRepository pricing,
        IUserExportRepository export,
        IUserDayTemplatesRepository days)
    {
        _user = user;
        _demand = demand;
        _shiftable = shiftable;
        _charge = charge;
        _pricing = pricing;
        _export = export;
        _days = days;
    }

    public async Task<IEnumerable<PowerAtAbsoluteTimes>> GetDemandProfiles()
    {
        return (await _demand.GetAsync(_user.Id)) ?? Enumerable.Empty<PowerAtAbsoluteTimes>();
    }

    public Task<IEnumerable<PowerAtAbsoluteTimes>> PutDemandProfiles(IEnumerable<PowerAtAbsoluteTimes> templates)
    {
        return _demand.UpsertAsync(_user.Id, templates);
    }

    public async Task<IEnumerable<PowerAtRelativeTimes>> GetShiftableDemands()
    {
        return (await _shiftable.GetAsync(_user.Id)) ?? Enumerable.Empty<PowerAtRelativeTimes>();
    }

    public Task<IEnumerable<PowerAtRelativeTimes>> PutShiftableDemands(IEnumerable<PowerAtRelativeTimes> templates)
    {
        return _shiftable.UpsertAsync(_user.Id, templates);
    }

    public async Task<IEnumerable<PowerAtAbsoluteTimes>> GetChargeProfiles()
    {
        return (await _charge.GetAsync(_user.Id)) ?? Enumerable.Empty<PowerAtAbsoluteTimes>();
    }

    public Task<IEnumerable<PowerAtAbsoluteTimes>> PutChargeProfiles(IEnumerable<PowerAtAbsoluteTimes> templates)
    {
        return _charge.UpsertAsync(_user.Id, templates);
    }

    public async Task<IEnumerable<PriceAtAbsoluteTimes>> GetPricingProfiles()
    {
        return (await _pricing.GetAsync(_user.Id)) ?? Enumerable.Empty<PriceAtAbsoluteTimes>();
    }

    public Task<IEnumerable<PriceAtAbsoluteTimes>> PutPricingProfiles(IEnumerable<PriceAtAbsoluteTimes> templates)
    {
        return _pricing.UpsertAsync(_user.Id, templates);
    }

    public async Task<IEnumerable<PriceAtAbsoluteTimes>> GetExportProfiles()
    {
        return (await _export.GetAsync(_user.Id)) ?? Enumerable.Empty<PriceAtAbsoluteTimes>();
    }

    public Task<IEnumerable<PriceAtAbsoluteTimes>> PutExportProfiles(IEnumerable<PriceAtAbsoluteTimes> templates)
    {
        return _export.UpsertAsync(_user.Id, templates);
    }

    public async Task<ChargePlanTemplatedParameters> GetDayTemplates()
    {
        return (await _days.GetAsync(_user.Id)) ?? new ChargePlanTemplatedParameters(new(), new());
    }

    public Task<ChargePlanTemplatedParameters> PutDayTemplates(ChargePlanTemplatedParameters template)
    {
        return _days.UpsertAsync(_user.Id, template);
    }

    public Task<DayTemplate> GetTomorrowsDemand() => GetRelativeDayDemand(1);
    public Task<DayTemplate> GetTodaysDemand() => GetRelativeDayDemand(0);
    public Task<DayTemplate> PutTomorrowsDemand(DayTemplate template) => PutRelativeDayDemand(template, 1);
    public Task<DayTemplate> PutTodaysDemand(DayTemplate template) => PutRelativeDayDemand(template, 0);

    private async Task<DayTemplate> PutRelativeDayDemand(DayTemplate template, int daysAhead)
    {
        DayOfWeek when = DateTime.Today.AddDays(daysAhead).DayOfWeek;
        template = template with { DayOfWeek = when };

        var days = await _days.GetAsync(_user.Id) ?? new(new(), new());

        days = days with { DayTemplates = days.DayTemplates.Where(f => f.DayOfWeek != when)
            .Append(template)
            .OrderBy(f => f.DayOfWeek)
            .ToList() };

        await _days.UpsertAsync(_user.Id, days);

        return template;
    }

    private async Task<DayTemplate> GetRelativeDayDemand(int daysAhead)
    {
        DayOfWeek when = DateTime.Today.AddDays(daysAhead).DayOfWeek;

        var days = await _days.GetAsync(_user.Id) ?? new(new(), new());
        var template = days.DayTemplates.SingleOrDefault(f=>f.DayOfWeek == when) ?? throw new NotFoundException();

        return template;
    }
}