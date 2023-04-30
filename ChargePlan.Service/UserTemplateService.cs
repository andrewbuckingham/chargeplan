public class UserTemplateService
{
    private readonly IUserDemandRepository _demand;
    private readonly IUserShiftableDemandRepository _shiftable;
    private readonly IUserChargeRepository _charge;
    private readonly IUserPricingRepository _pricing;
    private readonly IUserExportRepository _export;

    public UserTemplateService(
        IUserDemandRepository demand,
        IUserShiftableDemandRepository shiftable,
        IUserChargeRepository charge,
        IUserPricingRepository pricing,
        IUserExportRepository export)
    {
        _demand = demand;
        _shiftable = shiftable;
        _charge = charge;
        _pricing = pricing;
        _export = export;
    }

    public async Task<IEnumerable<PowerAtAbsoluteTimes>> GetDemandProfiles(Guid userId)
    {
        return (await _demand.GetAsync(userId)) ?? Enumerable.Empty<PowerAtAbsoluteTimes>();
    }

    public Task<IEnumerable<PowerAtAbsoluteTimes>> PutDemandProfiles(Guid userId, IEnumerable<PowerAtAbsoluteTimes> templates)
    {
        return _demand.UpsertAsync(userId, templates);
    }

    public async Task<IEnumerable<PowerAtRelativeTimes>> GetShiftableDemands(Guid userId)
    {
        return (await _shiftable.GetAsync(userId)) ?? Enumerable.Empty<PowerAtRelativeTimes>();
    }

    public Task<IEnumerable<PowerAtRelativeTimes>> PutShiftableDemands(Guid userId, IEnumerable<PowerAtRelativeTimes> templates)
    {
        return _shiftable.UpsertAsync(userId, templates);
    }

    public async Task<IEnumerable<PowerAtAbsoluteTimes>> GetChargeProfiles(Guid userId)
    {
        return (await _charge.GetAsync(userId)) ?? Enumerable.Empty<PowerAtAbsoluteTimes>();
    }

    public Task<IEnumerable<PowerAtAbsoluteTimes>> PutChargeProfiles(Guid userId, IEnumerable<PowerAtAbsoluteTimes> templates)
    {
        return _charge.UpsertAsync(userId, templates);
    }

    public async Task<IEnumerable<PriceAtAbsoluteTimes>> GetPricingProfiles(Guid userId)
    {
        return (await _pricing.GetAsync(userId)) ?? Enumerable.Empty<PriceAtAbsoluteTimes>();
    }

    public Task<IEnumerable<PriceAtAbsoluteTimes>> PutPricingProfiles(Guid userId, IEnumerable<PriceAtAbsoluteTimes> templates)
    {
        return _pricing.UpsertAsync(userId, templates);
    }

    public async Task<IEnumerable<PriceAtAbsoluteTimes>> GetExportProfiles(Guid userId)
    {
        return (await _export.GetAsync(userId)) ?? Enumerable.Empty<PriceAtAbsoluteTimes>();
    }

    public Task<IEnumerable<PriceAtAbsoluteTimes>> PutExportProfiles(Guid userId, IEnumerable<PriceAtAbsoluteTimes> templates)
    {
        return _export.UpsertAsync(userId, templates);
    }
}